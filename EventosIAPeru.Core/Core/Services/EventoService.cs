using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services
{
    /// <summary>US-04 (publicación y edición) y US-05 (búsqueda y filtrado).</summary>
    public class EventoService : IEventoService
    {
        private const string RolOrganizador = "ORGANIZADOR";

        private readonly IEventoRepository _eventoRepository;
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        public EventoService(IEventoRepository eventoRepository,
                             ICategoriaRepository categoriaRepository,
                             IUsuarioRepository usuarioRepository)
        {
            _eventoRepository = eventoRepository;
            _categoriaRepository = categoriaRepository;
            _usuarioRepository = usuarioRepository;
        }

        // ---------------------------- US-05 ----------------------------

        public async Task<(IEnumerable<EventoDTO> Eventos, int Total)> BuscarEventos(FiltroEventoDTO filtro)
        {
            filtro.FechaDesde = FechaHelper.AUtc(filtro.FechaDesde);
            filtro.FechaHasta = FechaHelper.FinDeDiaUtc(filtro.FechaHasta);

            // Alimenta los chips de "categorías más consultadas"
            if (filtro.CategoriaId.HasValue)
            {
                await _categoriaRepository.RegistrarConsulta(filtro.CategoriaId.Value);
            }

            var (eventos, total) = await _eventoRepository.BuscarEventos(filtro);
            var eventosDTO = await MapearEventos(eventos);
            return (eventosDTO, total);
        }

        public async Task<EventoDTO?> GetEventoById(int id, string? firebaseUid)
        {
            var evento = await _eventoRepository.GetEventoById(id);
            if (evento == null) return null;

            // Un borrador, cancelado o desactivado solo lo ve su organizador
            if (!EsVisibleAlPublico(evento))
            {
                if (string.IsNullOrWhiteSpace(firebaseUid)) return null;

                var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
                if (usuario == null || usuario.UsuarioId != evento.OrganizadorId) return null;
            }

            var vendidas = await _eventoRepository.GetEntradasVendidas(new[] { evento.EventoId });
            return MapearEvento(evento, vendidas.GetValueOrDefault(evento.EventoId));
        }

        // ---------------------------- US-04 ----------------------------

        public async Task<IEnumerable<EventoDTO>?> GetMisEventos(string firebaseUid)
        {
            var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (usuario == null) return null;

            var eventos = await _eventoRepository.GetEventosByOrganizador(usuario.UsuarioId);
            return await MapearEventos(eventos);
        }

        public async Task<ResultadoOperacion> CrearEvento(string firebaseUid, CrearEventoDTO eventoDTO)
        {
            var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (usuario == null) return ResultadoOperacion.NoAutorizado("Tu usuario no está registrado.");
            if (usuario.Estado != "ACTIVO") return ResultadoOperacion.NoAutorizado("Tu cuenta no está activa.");

            // US-03: para publicar eventos la cuenta debe tener el rol ORGANIZADOR
            var esOrganizador = await _usuarioRepository.TieneRol(usuario.UsuarioId, RolOrganizador);
            if (!esOrganizador)
                return ResultadoOperacion.NoAutorizado("Activa tu rol de organizador para crear eventos.");

            var categoria = await _categoriaRepository.GetCategoriaById(eventoDTO.CategoriaId);
            if (categoria == null) return ResultadoOperacion.Invalido("La categoría seleccionada no existe.");

            var fechaInicio = FechaHelper.AUtc(eventoDTO.FechaInicio);
            var fechaFin = FechaHelper.AUtc(eventoDTO.FechaFin);

            if (fechaInicio <= DateTime.UtcNow)
                return ResultadoOperacion.Invalido("La fecha del evento debe ser posterior a hoy.");
            if (fechaFin <= fechaInicio)
                return ResultadoOperacion.Invalido("La fecha de fin debe ser posterior a la de inicio.");

            var evento = new Evento();
            evento.OrganizadorId = usuario.UsuarioId;
            CopiarDatos(eventoDTO, evento, fechaInicio, fechaFin);
            evento.Estado = "BORRADOR";
            evento.EstadoModeracion = "EN_REVISION";
            evento.FechaCreacion = DateTime.UtcNow;
            evento.FechaActualizacion = DateTime.UtcNow;

            var creado = await _eventoRepository.CreateEvento(evento);
            if (!creado) return ResultadoOperacion.Invalido("No se pudo registrar el evento.");

            return ResultadoOperacion.Ok(evento.EventoId);
        }

        public async Task<ResultadoOperacion> ActualizarEvento(string firebaseUid, ActualizarEventoDTO eventoDTO)
        {
            var (evento, error) = await ObtenerEventoPropio(firebaseUid, eventoDTO.EventoId);
            if (error != null) return error;

            if (evento!.Estado == "CANCELADO")
                return ResultadoOperacion.Invalido("No se puede editar un evento cancelado.");
            if (evento.EstadoModeracion == "DESACTIVADO")
                return ResultadoOperacion.Invalido("El evento fue desactivado por moderación y no se puede editar.");

            var categoria = await _categoriaRepository.GetCategoriaById(eventoDTO.CategoriaId);
            if (categoria == null) return ResultadoOperacion.Invalido("La categoría seleccionada no existe.");

            var fechaInicio = FechaHelper.AUtc(eventoDTO.FechaInicio);
            var fechaFin = FechaHelper.AUtc(eventoDTO.FechaFin);

            if (fechaInicio != evento.FechaInicio && fechaInicio <= DateTime.UtcNow)
                return ResultadoOperacion.Invalido("La nueva fecha del evento debe ser posterior a hoy.");
            if (fechaFin <= fechaInicio)
                return ResultadoOperacion.Invalido("La fecha de fin debe ser posterior a la de inicio.");

            var vendidas = await _eventoRepository.GetEntradasVendidas(new[] { evento.EventoId });
            var entradasVendidas = vendidas.GetValueOrDefault(evento.EventoId);
            if (eventoDTO.AforoTotal < entradasVendidas)
                return ResultadoOperacion.Invalido($"El aforo no puede ser menor a las {entradasVendidas} entradas ya vendidas.");

            var eventoActualizado = new Evento();
            eventoActualizado.EventoId = evento.EventoId;
            CopiarDatos(eventoDTO, eventoActualizado, fechaInicio, fechaFin);

            var actualizado = await _eventoRepository.UpdateEvento(eventoActualizado);
            if (!actualizado) return ResultadoOperacion.Invalido("No se pudo actualizar el evento.");

            return ResultadoOperacion.Ok(evento.EventoId);
        }

        public async Task<ResultadoOperacion> PublicarEvento(string firebaseUid, int id)
        {
            var (evento, error) = await ObtenerEventoPropio(firebaseUid, id);
            if (error != null) return error;

            if (evento!.Estado != "BORRADOR")
                return ResultadoOperacion.Invalido("Solo se puede publicar un evento en borrador.");
            if (evento.EstadoModeracion == "DESACTIVADO")
                return ResultadoOperacion.Invalido("El evento fue desactivado por moderación.");
            if (evento.FechaInicio <= DateTime.UtcNow)
                return ResultadoOperacion.Invalido("No se puede publicar un evento con fecha pasada.");

            var actualizado = await _eventoRepository.UpdateEstado(id, "PUBLICADO");
            if (!actualizado) return ResultadoOperacion.Invalido("No se pudo publicar el evento.");

            return ResultadoOperacion.Ok(id);
        }

        public async Task<ResultadoOperacion> CancelarEvento(string firebaseUid, int id)
        {
            var (evento, error) = await ObtenerEventoPropio(firebaseUid, id);
            if (error != null) return error;

            if (evento!.Estado == "CANCELADO")
                return ResultadoOperacion.Invalido("El evento ya está cancelado.");
            if (evento.FechaFin <= DateTime.UtcNow)
                return ResultadoOperacion.Invalido("No se puede cancelar un evento que ya terminó.");

            var actualizado = await _eventoRepository.UpdateEstado(id, "CANCELADO");
            if (!actualizado) return ResultadoOperacion.Invalido("No se pudo cancelar el evento.");

            return ResultadoOperacion.Ok(id);
        }

        // ---------------------------- Ayudantes ----------------------------

        /// <summary>El organizador solo puede modificar sus propios eventos (US-03 / US-04).</summary>
        private async Task<(Evento? Evento, ResultadoOperacion? Error)> ObtenerEventoPropio(string firebaseUid, int eventoId)
        {
            var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (usuario == null)
                return (null, ResultadoOperacion.NoAutorizado("Tu usuario no está registrado."));
            if (usuario.Estado != "ACTIVO")
                return (null, ResultadoOperacion.NoAutorizado("Tu cuenta no está activa."));

            var evento = await _eventoRepository.GetEventoById(eventoId);
            if (evento == null)
                return (null, ResultadoOperacion.NoEncontrado("El evento no existe."));
            if (evento.OrganizadorId != usuario.UsuarioId)
                return (null, ResultadoOperacion.NoAutorizado("Solo puedes modificar tus propios eventos."));

            return (evento, null);
        }

        private static bool EsVisibleAlPublico(Evento evento)
        {
            return evento.Estado == "PUBLICADO" && evento.EstadoModeracion != "DESACTIVADO";
        }

        private static void CopiarDatos(CrearEventoDTO origen, Evento destino, DateTime fechaInicioUtc, DateTime fechaFinUtc)
        {
            destino.CategoriaId = origen.CategoriaId;
            destino.Nombre = origen.Nombre.Trim();
            destino.Descripcion = origen.Descripcion.Trim();
            destino.ImagenUrl = string.IsNullOrWhiteSpace(origen.ImagenUrl) ? null : origen.ImagenUrl.Trim();
            destino.FechaInicio = fechaInicioUtc;
            destino.FechaFin = fechaFinUtc;
            destino.Sede = origen.Sede.Trim();
            destino.Direccion = origen.Direccion.Trim();
            destino.Departamento = origen.Departamento.Trim();
            destino.Provincia = origen.Provincia.Trim();
            destino.Distrito = origen.Distrito.Trim();
            destino.AforoTotal = origen.AforoTotal;
            destino.Precio = origen.Precio;
        }

        private async Task<List<EventoDTO>> MapearEventos(IEnumerable<Evento> eventos)
        {
            var lista = eventos.ToList();
            var vendidas = await _eventoRepository.GetEntradasVendidas(lista.Select(e => e.EventoId));
            var eventosDTO = new List<EventoDTO>();

            foreach (var evento in lista)
            {
                eventosDTO.Add(MapearEvento(evento, vendidas.GetValueOrDefault(evento.EventoId)));
            }
            return eventosDTO;
        }

        private static EventoDTO MapearEvento(Evento evento, int entradasVendidas)
        {
            var ahora = DateTime.UtcNow;
            var cupos = Math.Max(evento.AforoTotal - entradasVendidas, 0);

            var eventoDTO = new EventoDTO();
            eventoDTO.EventoId = evento.EventoId;
            eventoDTO.OrganizadorId = evento.OrganizadorId;
            eventoDTO.OrganizadorNombre = evento.Organizador?.Nombre;
            eventoDTO.CategoriaId = evento.CategoriaId;
            eventoDTO.CategoriaNombre = evento.Categoria?.Nombre;
            eventoDTO.Nombre = evento.Nombre;
            eventoDTO.Descripcion = evento.Descripcion;
            eventoDTO.ImagenUrl = evento.ImagenUrl;
            eventoDTO.FechaInicio = evento.FechaInicio;
            eventoDTO.FechaFin = evento.FechaFin;
            eventoDTO.Sede = evento.Sede;
            eventoDTO.Direccion = evento.Direccion;
            eventoDTO.Departamento = evento.Departamento;
            eventoDTO.Provincia = evento.Provincia;
            eventoDTO.Distrito = evento.Distrito;
            eventoDTO.AforoTotal = evento.AforoTotal;
            eventoDTO.EntradasVendidas = entradasVendidas;
            eventoDTO.CuposDisponibles = cupos;
            eventoDTO.Precio = evento.Precio;
            eventoDTO.Estado = evento.Estado;
            eventoDTO.EstadoModeracion = evento.EstadoModeracion;
            eventoDTO.Agotado = cupos == 0;
            eventoDTO.Finalizado = evento.FechaFin <= ahora;
            eventoDTO.DisponibleParaCompra = EsVisibleAlPublico(evento) && cupos > 0 && evento.FechaInicio > ahora;
            return eventoDTO;
        }
    }
}
