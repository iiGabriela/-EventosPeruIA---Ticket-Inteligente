using System.Text;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services
{
    /// <summary>
    /// Service: reglas de US-15 (moderación de eventos y usuarios).
    /// Antes de cada acción se revisa que quien la pide sea ADMINISTRADOR.
    /// Cada acción importante deja un registro en la tabla de auditoría.
    /// </summary>
    public class ModeracionService : IModeracionService
    {
        private const string RolAdministrador = "ADMINISTRADOR";

        // Valores permitidos (los mismos que acepta la base de datos)
        private static readonly string[] EstadosModeracion = { "EN_REVISION", "CONFORME", "DESACTIVADO" };
        private static readonly string[] EstadosUsuario = { "ACTIVO", "BLOQUEADO", "INACTIVO" };
        private static readonly string[] EstadosReporte = { "RESUELTO", "DESCARTADO" };
        private static readonly string[] CamposRegla = { "AFORO_TOTAL", "PRECIO" };
        private static readonly string[] OperadoresRegla = { "MAYOR", "MAYOR_IGUAL", "MENOR", "MENOR_IGUAL", "IGUAL" };

        private const int TamanoPagina = 20;

        private readonly IModeracionRepository _moderacionRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        public ModeracionService(IModeracionRepository moderacionRepository, IUsuarioRepository usuarioRepository)
        {
            _moderacionRepository = moderacionRepository;
            _usuarioRepository = usuarioRepository;
        }

        // ---------------------------- Panel ----------------------------

        public async Task<(ResultadoOperacion Resultado, PanelModeracionDTO? Panel)> GetPanel(string firebaseUid)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return (error, null);

            var ind = await _moderacionRepository.GetIndicadores();

            var panel = new PanelModeracionDTO
            {
                UsuariosActivos = ind.UsuariosActivos,
                EventosPublicados = ind.EventosPublicados,
                RevisionesPendientes = ind.RevisionesPendientes,
                CuentasSuspendidas = ind.CuentasSuspendidas,
                ReportesPendientes = ind.ReportesPendientes,
                RegistrosAuditoria = ind.RegistrosAuditoria,
                AuditoriaEstado = "Activo"
            };
            return (ResultadoOperacion.Ok(admin!.UsuarioId), panel);
        }

        // ---------------------------- Eventos ----------------------------

        public async Task<(ResultadoOperacion Resultado, ModeracionEventosDTO? Pagina)> BuscarEventos(string firebaseUid, string? estadoModeracion, string? texto, int pagina)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return (error, null);

            if (pagina < 1) pagina = 1;
            var estado = estadoModeracion?.Trim().ToUpperInvariant();

            var (eventos, total) = await _moderacionRepository.BuscarEventos(estado, texto, pagina, TamanoPagina);

            var resultado = new ModeracionEventosDTO
            {
                Total = total,
                Pagina = pagina,
                Eventos = eventos.Select(e => new ModeracionEventoDTO
                {
                    EventoId = e.EventoId,
                    Nombre = e.Nombre,
                    OrganizadorNombre = e.Organizador.Nombre,
                    Estado = e.Estado,
                    EstadoModeracion = e.EstadoModeracion,
                    MotivoModeracion = e.MotivoModeracion,
                    AforoTotal = e.AforoTotal,
                    FechaInicio = e.FechaInicio,
                    ReportesPendientes = e.ReporteEvento.Count(r => r.Estado == "PENDIENTE")
                }).ToList()
            };
            return (ResultadoOperacion.Ok(admin!.UsuarioId), resultado);
        }

        public async Task<(ResultadoOperacion Resultado, DetalleEventoModeracionDTO? Detalle)> GetDetalleEvento(string firebaseUid, int eventoId)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return (error, null);

            var evento = await _moderacionRepository.GetEventoDetalle(eventoId);
            if (evento == null) return (ResultadoOperacion.NoEncontrado("El evento no existe."), null);

            var detalle = new DetalleEventoModeracionDTO
            {
                EventoId = evento.EventoId,
                Nombre = evento.Nombre,
                OrganizadorNombre = evento.Organizador.Nombre,
                Estado = evento.Estado,
                EstadoModeracion = evento.EstadoModeracion,
                MotivoModeracion = evento.MotivoModeracion,
                ValidacionSunat = evento.ValidacionSunat,
                ValidacionMunicipal = evento.ValidacionMunicipal,
                AforoTotal = evento.AforoTotal,
                ReportesActivos = evento.ReporteEvento.Count(r => r.Estado == "PENDIENTE"),
                Reportes = evento.ReporteEvento
                                 .OrderByDescending(r => r.FechaReporte)
                                 .Select(r => MapearReporte(r, evento.Nombre))
                                 .ToList()
            };
            return (ResultadoOperacion.Ok(evento.EventoId), detalle);
        }

        public async Task<ResultadoOperacion> ModerarEvento(string firebaseUid, int eventoId, ModerarEventoDTO dto)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return error;

            // Pedimos confirmación antes de moderar
            if (!dto.Confirmado)
                return ResultadoOperacion.Invalido("Debes confirmar la acción de moderación.");

            var nuevoEstado = dto.EstadoModeracion.Trim().ToUpperInvariant();
            if (!EstadosModeracion.Contains(nuevoEstado))
                return ResultadoOperacion.Invalido("El estado debe ser EN_REVISION, CONFORME o DESACTIVADO.");

            var motivo = dto.Motivo?.Trim();

            // Para desactivar un evento hay que explicar por qué
            if (nuevoEstado == "DESACTIVADO" && (string.IsNullOrWhiteSpace(motivo) || motivo.Length < 5))
                return ResultadoOperacion.Invalido("Indica el motivo (mínimo 5 caracteres) para desactivar el evento.");

            var evento = await _moderacionRepository.GetEventoParaModerar(eventoId);
            if (evento == null) return ResultadoOperacion.NoEncontrado("El evento no existe.");

            // Si aprueba sin motivo, ponemos uno por defecto
            if (string.IsNullOrWhiteSpace(motivo) && nuevoEstado == "CONFORME")
                motivo = "Documentación validada";

            evento.EstadoModeracion = nuevoEstado;
            evento.MotivoModeracion = string.IsNullOrWhiteSpace(motivo) ? null : motivo;
            evento.FechaActualizacion = DateTime.UtcNow;

            var auditoria = NuevaAuditoria(admin!.UsuarioId, "EVENTO_" + nuevoEstado, motivo);
            auditoria.EventoId = evento.EventoId;
            _moderacionRepository.AgregarAuditoria(auditoria);

            var guardado = await _moderacionRepository.GuardarCambios();
            if (!guardado) return ResultadoOperacion.Invalido("No se pudo guardar la moderación.");

            return ResultadoOperacion.Ok(evento.EventoId);
        }

        // ---------------------------- Reportes ----------------------------

        // Este lo puede usar cualquier usuario activo (no hace falta ser admin)
        public async Task<ResultadoOperacion> ReportarEvento(string firebaseUid, int eventoId, CrearReporteDTO dto)
        {
            var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (usuario == null) return ResultadoOperacion.NoAutorizado("Tu usuario no está registrado.");
            if (usuario.Estado != "ACTIVO") return ResultadoOperacion.NoAutorizado("Tu cuenta no está activa.");

            var motivo = dto.Motivo.Trim();
            if (motivo.Length < 5)
                return ResultadoOperacion.Invalido("El motivo debe tener al menos 5 caracteres.");

            if (!await _moderacionRepository.ExisteEvento(eventoId))
                return ResultadoOperacion.NoEncontrado("El evento no existe.");

            if (await _moderacionRepository.ExisteReportePendiente(usuario.UsuarioId, eventoId))
                return ResultadoOperacion.Invalido("Ya reportaste este evento y está pendiente de revisión.");

            // Un reporte de usuario lleva su id y no lleva regla (lo exige la base de datos)
            var reporte = new ReporteEvento
            {
                EventoId = eventoId,
                ReportadoPorUsuarioId = usuario.UsuarioId,
                Origen = "USUARIO",
                Motivo = motivo,
                Estado = "PENDIENTE",
                FechaReporte = DateTime.UtcNow
            };
            _moderacionRepository.AgregarReporte(reporte);

            var guardado = await _moderacionRepository.GuardarCambios();
            if (!guardado) return ResultadoOperacion.Invalido("No se pudo registrar el reporte.");

            return ResultadoOperacion.Ok(reporte.ReporteId);
        }

        public async Task<(ResultadoOperacion Resultado, List<ReporteDTO>? Reportes)> GetReportes(string firebaseUid, string? estado)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return (error, null);

            var reportes = await _moderacionRepository.GetReportes(estado?.Trim().ToUpperInvariant());
            return (ResultadoOperacion.Ok(admin!.UsuarioId), reportes.Select(r => MapearReporte(r, r.Evento?.Nombre)).ToList());
        }

        public async Task<ResultadoOperacion> ResolverReporte(string firebaseUid, int reporteId, ResolverReporteDTO dto)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return error;

            if (!dto.Confirmado)
                return ResultadoOperacion.Invalido("Debes confirmar la acción de moderación.");

            var estado = dto.Estado.Trim().ToUpperInvariant();
            if (!EstadosReporte.Contains(estado))
                return ResultadoOperacion.Invalido("El estado debe ser RESUELTO o DESCARTADO.");

            var resolucion = dto.Resolucion.Trim();
            if (resolucion.Length == 0)
                return ResultadoOperacion.Invalido("La resolución es obligatoria.");

            var reporte = await _moderacionRepository.GetReporteParaResolver(reporteId);
            if (reporte == null) return ResultadoOperacion.NoEncontrado("El reporte no existe.");
            if (reporte.Estado != "PENDIENTE")
                return ResultadoOperacion.Invalido("Este reporte ya fue resuelto.");

            // La base exige que la fecha de resolución no sea anterior a la del reporte
            var ahora = DateTime.UtcNow;
            reporte.Estado = estado;
            reporte.FechaResolucion = ahora < reporte.FechaReporte ? reporte.FechaReporte : ahora;
            reporte.ResueltoPorUsuarioId = admin!.UsuarioId;
            reporte.Resolucion = resolucion;

            var auditoria = NuevaAuditoria(admin.UsuarioId, "REPORTE_" + estado, resolucion);
            auditoria.EventoId = reporte.EventoId;
            _moderacionRepository.AgregarAuditoria(auditoria);

            var guardado = await _moderacionRepository.GuardarCambios();
            if (!guardado) return ResultadoOperacion.Invalido("No se pudo guardar la resolución.");

            return ResultadoOperacion.Ok(reporte.ReporteId);
        }

        // ---------------------------- Usuarios ----------------------------

        public async Task<(ResultadoOperacion Resultado, List<UsuarioDTO>? Usuarios)> BuscarUsuarios(string firebaseUid, string? texto, string? estado)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return (error, null);

            var usuarios = await _moderacionRepository.BuscarUsuarios(texto, estado?.Trim().ToUpperInvariant(), 50);

            var lista = usuarios.Select(u => new UsuarioDTO
            {
                UsuarioId = u.UsuarioId,
                Nombre = u.Nombre,
                Email = u.Email,
                Estado = u.Estado,
                Verificado = u.Verificado,
                FechaRegistro = u.FechaRegistro,
                Roles = u.Rol.Select(r => r.Nombre).OrderBy(n => n).ToList()
            }).ToList();

            return (ResultadoOperacion.Ok(admin!.UsuarioId), lista);
        }

        public async Task<ResultadoOperacion> CambiarEstadoUsuario(string firebaseUid, int usuarioId, CambiarEstadoUsuarioDTO dto)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return error;

            if (!dto.Confirmado)
                return ResultadoOperacion.Invalido("Debes confirmar la acción de moderación.");

            var estado = dto.Estado.Trim().ToUpperInvariant();
            if (!EstadosUsuario.Contains(estado))
                return ResultadoOperacion.Invalido("El estado debe ser ACTIVO, BLOQUEADO o INACTIVO.");

            // Un admin no puede bloquearse a sí mismo por error
            if (usuarioId == admin!.UsuarioId)
                return ResultadoOperacion.Invalido("No puedes cambiar el estado de tu propia cuenta.");

            var motivo = dto.Motivo?.Trim();
            if (estado != "ACTIVO" && (string.IsNullOrWhiteSpace(motivo) || motivo.Length < 5))
                return ResultadoOperacion.Invalido("Indica el motivo (mínimo 5 caracteres) para bloquear o desactivar la cuenta.");

            var usuario = await _moderacionRepository.GetUsuarioParaModerar(usuarioId);
            if (usuario == null) return ResultadoOperacion.NoEncontrado("El usuario no existe.");

            usuario.Estado = estado;

            var auditoria = NuevaAuditoria(admin.UsuarioId, "USUARIO_" + estado, motivo);
            auditoria.UsuarioAfectadoId = usuario.UsuarioId;
            _moderacionRepository.AgregarAuditoria(auditoria);

            var guardado = await _moderacionRepository.GuardarCambios();
            if (!guardado) return ResultadoOperacion.Invalido("No se pudo cambiar el estado del usuario.");

            return ResultadoOperacion.Ok(usuario.UsuarioId);
        }

        public async Task<ResultadoOperacion> VerificarUsuario(string firebaseUid, int usuarioId)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return error;

            var usuario = await _moderacionRepository.GetUsuarioParaModerar(usuarioId);
            if (usuario == null) return ResultadoOperacion.NoEncontrado("El usuario no existe.");

            usuario.Verificado = true;

            var auditoria = NuevaAuditoria(admin!.UsuarioId, "USUARIO_VERIFICADO", null);
            auditoria.UsuarioAfectadoId = usuario.UsuarioId;
            _moderacionRepository.AgregarAuditoria(auditoria);

            var guardado = await _moderacionRepository.GuardarCambios();
            if (!guardado) return ResultadoOperacion.Invalido("No se pudo verificar al usuario.");

            return ResultadoOperacion.Ok(usuario.UsuarioId);
        }

        // ---------------------------- Auditoría ----------------------------

        public async Task<(ResultadoOperacion Resultado, List<AuditoriaDTO>? Registros)> GetAuditoria(string firebaseUid, DateTime? desde, DateTime? hasta)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return (error, null);

            var registros = await _moderacionRepository.GetAuditoria(AUtc(desde), FinDeDiaUtc(hasta), 200);
            return (ResultadoOperacion.Ok(admin!.UsuarioId), registros.Select(MapearAuditoria).ToList());
        }

        // Genera un CSV (se abre en Excel) con el log de auditoría
        public async Task<(ResultadoOperacion Resultado, byte[]? Archivo)> ExportarAuditoria(string firebaseUid)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return (error, null);

            var registros = await _moderacionRepository.GetAuditoria(null, null, 5000);

            var sb = new StringBuilder();
            sb.AppendLine("Id;Fecha (UTC);Actor;Accion;Detalle;EventoId;UsuarioAfectadoId;ReglaId");
            foreach (var a in registros)
            {
                sb.AppendLine(string.Join(";", new[]
                {
                    a.AuditoriaId.ToString(),
                    a.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
                    CsvTexto(a.ActorUsuario?.Nombre),
                    CsvTexto(a.Accion),
                    CsvTexto(a.Detalle),
                    a.EventoId?.ToString() ?? "",
                    a.UsuarioAfectadoId?.ToString() ?? "",
                    a.ReglaId?.ToString() ?? ""
                }));
            }

            // UTF-8 con BOM para que Excel muestre bien las tildes
            var utf8 = new UTF8Encoding(true);
            var bytes = utf8.GetPreamble().Concat(utf8.GetBytes(sb.ToString())).ToArray();
            return (ResultadoOperacion.Ok(admin!.UsuarioId), bytes);
        }

        // ---------------------------- Reglas ----------------------------

        public async Task<(ResultadoOperacion Resultado, List<ReglaModeracionDTO>? Reglas)> GetReglas(string firebaseUid)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return (error, null);

            var reglas = await _moderacionRepository.GetReglas();
            return (ResultadoOperacion.Ok(admin!.UsuarioId), reglas.Select(MapearRegla).ToList());
        }

        public async Task<ResultadoOperacion> CrearRegla(string firebaseUid, CrearReglaDTO dto)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return error;

            var nombre = dto.Nombre.Trim();
            var criterio = dto.Criterio.Trim();
            var tipo = dto.Tipo.Trim().ToUpperInvariant();
            var accion = dto.Accion.Trim().ToUpperInvariant();

            if (nombre.Length == 0 || criterio.Length == 0)
                return ResultadoOperacion.Invalido("El nombre y el criterio son obligatorios.");
            if (tipo != "SIMPLE" && tipo != "IA")
                return ResultadoOperacion.Invalido("El tipo debe ser SIMPLE o IA.");
            if (accion != "EN_REVISION" && accion != "DESACTIVADO")
                return ResultadoOperacion.Invalido("La acción debe ser EN_REVISION o DESACTIVADO.");

            var regla = new ReglaModeracion
            {
                Nombre = nombre,
                Tipo = tipo,
                Criterio = criterio,
                Accion = accion,
                Activa = true,
                CreadaPorUsuarioId = admin!.UsuarioId
            };

            if (tipo == "SIMPLE")
            {
                // Regla simple: compara un campo (aforo o precio) contra un valor
                var campo = dto.Campo?.Trim().ToUpperInvariant();
                var operador = dto.Operador?.Trim().ToUpperInvariant();

                if (campo == null || !CamposRegla.Contains(campo))
                    return ResultadoOperacion.Invalido("El campo debe ser AFORO_TOTAL o PRECIO.");
                if (operador == null || !OperadoresRegla.Contains(operador))
                    return ResultadoOperacion.Invalido("El operador debe ser MAYOR, MAYOR_IGUAL, MENOR, MENOR_IGUAL o IGUAL.");
                if (!dto.Valor.HasValue || dto.Valor.Value < 0)
                    return ResultadoOperacion.Invalido("El valor es obligatorio y no puede ser negativo.");

                regla.Campo = campo;
                regla.Operador = operador;
                regla.Valor = dto.Valor.Value;
            }
            // Regla de IA: solo lleva el criterio en texto (campo, operador y valor quedan vacíos)

            _moderacionRepository.AgregarRegla(regla);

            var auditoria = NuevaAuditoria(admin.UsuarioId, "REGLA_CREADA", nombre);
            auditoria.Regla = regla;
            _moderacionRepository.AgregarAuditoria(auditoria);

            var guardado = await _moderacionRepository.GuardarCambios();
            if (!guardado) return ResultadoOperacion.Invalido("No se pudo crear la regla.");

            return ResultadoOperacion.Ok(regla.ReglaId);
        }

        public async Task<ResultadoOperacion> CambiarEstadoRegla(string firebaseUid, int reglaId, bool activa)
        {
            var (admin, error) = await ObtenerAdmin(firebaseUid);
            if (error != null) return error;

            var regla = await _moderacionRepository.GetReglaParaEditar(reglaId);
            if (regla == null) return ResultadoOperacion.NoEncontrado("La regla no existe.");

            regla.Activa = activa;

            var auditoria = NuevaAuditoria(admin!.UsuarioId, activa ? "REGLA_ACTIVADA" : "REGLA_DESACTIVADA", regla.Nombre);
            auditoria.ReglaId = regla.ReglaId;
            _moderacionRepository.AgregarAuditoria(auditoria);

            var guardado = await _moderacionRepository.GuardarCambios();
            if (!guardado) return ResultadoOperacion.Invalido("No se pudo cambiar la regla.");

            return ResultadoOperacion.Ok(regla.ReglaId);
        }

        // ---------------------------- Ayudantes ----------------------------

        // Revisa que el usuario exista, esté activo y tenga el rol ADMINISTRADOR
        private async Task<(Usuario? Admin, ResultadoOperacion? Error)> ObtenerAdmin(string firebaseUid)
        {
            var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (usuario == null)
                return (null, ResultadoOperacion.NoAutorizado("Tu usuario no está registrado."));
            if (usuario.Estado != "ACTIVO")
                return (null, ResultadoOperacion.NoAutorizado("Tu cuenta no está activa."));

            var esAdmin = await _usuarioRepository.TieneRol(usuario.UsuarioId, RolAdministrador);
            if (!esAdmin)
                return (null, ResultadoOperacion.NoAutorizado("Solo un administrador puede realizar esta acción."));

            return (usuario, null);
        }

        // Crea un registro de auditoría (después se le agrega el evento, usuario o regla afectada)
        private static Auditoria NuevaAuditoria(int actorId, string accion, string? detalle)
        {
            return new Auditoria
            {
                ActorUsuarioId = actorId,
                Accion = accion,
                Detalle = detalle,
                Fecha = DateTime.UtcNow
            };
        }

        private static ReporteDTO MapearReporte(ReporteEvento r, string? eventoNombre)
        {
            return new ReporteDTO
            {
                ReporteId = r.ReporteId,
                EventoId = r.EventoId,
                EventoNombre = eventoNombre,
                Origen = r.Origen,
                Motivo = r.Motivo,
                Estado = r.Estado,
                ReportadoPor = r.ReportadoPorUsuario?.Nombre ?? r.Origen,
                FechaReporte = r.FechaReporte,
                FechaResolucion = r.FechaResolucion,
                Resolucion = r.Resolucion
            };
        }

        private static AuditoriaDTO MapearAuditoria(Auditoria a)
        {
            return new AuditoriaDTO
            {
                AuditoriaId = a.AuditoriaId,
                Fecha = a.Fecha,
                Actor = a.ActorUsuario?.Nombre,
                Accion = a.Accion,
                Detalle = a.Detalle,
                EventoId = a.EventoId,
                UsuarioAfectadoId = a.UsuarioAfectadoId,
                ReglaId = a.ReglaId
            };
        }

        private static ReglaModeracionDTO MapearRegla(ReglaModeracion r)
        {
            return new ReglaModeracionDTO
            {
                ReglaId = r.ReglaId,
                Nombre = r.Nombre,
                Tipo = r.Tipo,
                Criterio = r.Criterio,
                Accion = r.Accion,
                Activa = r.Activa,
                Campo = r.Campo,
                Operador = r.Operador,
                Valor = r.Valor
            };
        }

        // Fechas en UTC (PostgreSQL solo acepta UTC en timestamptz)
        private static DateTime? AUtc(DateTime? fecha)
        {
            if (!fecha.HasValue) return null;
            return DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc);
        }

        // "hasta" incluye todo ese día
        private static DateTime? FinDeDiaUtc(DateTime? fecha)
        {
            if (!fecha.HasValue) return null;
            return DateTime.SpecifyKind(fecha.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        }

        // Protege el CSV: comillas dobles y sin saltos de línea
        private static string CsvTexto(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            var limpio = texto.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            return "\"" + limpio + "\"";
        }
    }
}
