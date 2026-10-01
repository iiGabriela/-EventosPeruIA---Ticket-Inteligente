using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories
{
    /// <summary>Repository de moderación (US-15). Solo consulta y guarda en la base de datos.</summary>
    public class ModeracionRepository : IModeracionRepository
    {
        private readonly EventosPeruIAContext _dbContext;

        public ModeracionRepository(EventosPeruIAContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ---------------------------- Panel ----------------------------

        // Cuenta usuarios, eventos y reportes para los indicadores del panel
        public async Task<(int UsuariosActivos, int EventosPublicados, int RevisionesPendientes, int CuentasSuspendidas, int ReportesPendientes, int RegistrosAuditoria)> GetIndicadores()
        {
            var usuariosActivos = await _dbContext.Usuario.CountAsync(u => u.Estado == "ACTIVO");
            var eventosPublicados = await _dbContext.Evento.CountAsync(e => e.Estado == "PUBLICADO");

            // Revisión pendiente = evento ya publicado que todavía no fue aprobado
            var revisionesPendientes = await _dbContext.Evento
                                                .CountAsync(e => e.Estado == "PUBLICADO" && e.EstadoModeracion == "EN_REVISION");

            var cuentasSuspendidas = await _dbContext.Usuario
                                                .CountAsync(u => u.Estado == "BLOQUEADO" || u.Estado == "INACTIVO");
            var reportesPendientes = await _dbContext.ReporteEvento.CountAsync(r => r.Estado == "PENDIENTE");
            var registrosAuditoria = await _dbContext.Auditoria.CountAsync();

            return (usuariosActivos, eventosPublicados, revisionesPendientes, cuentasSuspendidas, reportesPendientes, registrosAuditoria);
        }

        // ---------------------------- Eventos ----------------------------

        public async Task<(List<Evento> Eventos, int Total)> BuscarEventos(string? estadoModeracion, string? texto, int pagina, int tamano)
        {
            var query = _dbContext.Evento.AsNoTracking().AsQueryable();

            // Filtros opcionales
            if (!string.IsNullOrWhiteSpace(estadoModeracion))
                query = query.Where(e => e.EstadoModeracion == estadoModeracion);

            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim().ToLower();
                query = query.Where(e => e.Nombre.ToLower().Contains(t));
            }

            var total = await query.CountAsync();

            var eventos = await query
                                .Include(e => e.Organizador)
                                .Include(e => e.ReporteEvento)
                                .OrderByDescending(e => e.FechaCreacion)
                                .Skip((pagina - 1) * tamano)
                                .Take(tamano)
                                .ToListAsync();

            return (eventos, total);
        }

        public async Task<Evento?> GetEventoDetalle(int eventoId)
        {
            return await _dbContext
                            .Evento
                            .AsNoTracking()
                            .Include(e => e.Organizador)
                            .Include(e => e.ReporteEvento)
                                .ThenInclude(r => r.ReportadoPorUsuario)
                            .FirstOrDefaultAsync(e => e.EventoId == eventoId);
        }

        public async Task<Evento?> GetEventoParaModerar(int eventoId)
        {
            return await _dbContext.Evento.FirstOrDefaultAsync(e => e.EventoId == eventoId);
        }

        // ---------------------------- Reportes ----------------------------

        public async Task<List<ReporteEvento>> GetReportes(string? estado)
        {
            var query = _dbContext
                            .ReporteEvento
                            .AsNoTracking()
                            .Include(r => r.Evento)
                            .Include(r => r.ReportadoPorUsuario)
                            .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(r => r.Estado == estado);

            return await query.OrderByDescending(r => r.FechaReporte).ToListAsync();
        }

        public async Task<ReporteEvento?> GetReporteParaResolver(int reporteId)
        {
            return await _dbContext.ReporteEvento.FirstOrDefaultAsync(r => r.ReporteId == reporteId);
        }

        public async Task<bool> ExisteEvento(int eventoId)
        {
            return await _dbContext.Evento.AnyAsync(e => e.EventoId == eventoId);
        }

        // Evita que la misma persona reporte dos veces el mismo evento mientras siga pendiente
        public async Task<bool> ExisteReportePendiente(int usuarioId, int eventoId)
        {
            return await _dbContext
                            .ReporteEvento
                            .AnyAsync(r => r.ReportadoPorUsuarioId == usuarioId
                                        && r.EventoId == eventoId
                                        && r.Estado == "PENDIENTE");
        }

        public void AgregarReporte(ReporteEvento reporte)
        {
            _dbContext.ReporteEvento.Add(reporte);
        }

        // ---------------------------- Usuarios ----------------------------

        public async Task<List<Usuario>> BuscarUsuarios(string? texto, string? estado, int max)
        {
            var query = _dbContext.Usuario.AsNoTracking().Include(u => u.Rol).AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(u => u.Estado == estado);

            // Busca por nombre o por correo
            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim().ToLower();
                query = query.Where(u => u.Nombre.ToLower().Contains(t) || u.Email.ToLower().Contains(t));
            }

            return await query.OrderBy(u => u.Nombre).Take(max).ToListAsync();
        }

        public async Task<Usuario?> GetUsuarioParaModerar(int usuarioId)
        {
            return await _dbContext.Usuario.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
        }

        // ---------------------------- Auditoría ----------------------------

        public async Task<List<Auditoria>> GetAuditoria(DateTime? desde, DateTime? hasta, int max)
        {
            var query = _dbContext.Auditoria.AsNoTracking().Include(a => a.ActorUsuario).AsQueryable();

            if (desde.HasValue) query = query.Where(a => a.Fecha >= desde.Value);
            if (hasta.HasValue) query = query.Where(a => a.Fecha <= hasta.Value);

            return await query.OrderByDescending(a => a.Fecha).Take(max).ToListAsync();
        }

        public void AgregarAuditoria(Auditoria auditoria)
        {
            _dbContext.Auditoria.Add(auditoria);
        }

        // ---------------------------- Reglas ----------------------------

        public async Task<List<ReglaModeracion>> GetReglas()
        {
            return await _dbContext.ReglaModeracion.AsNoTracking().OrderBy(r => r.ReglaId).ToListAsync();
        }

        public async Task<ReglaModeracion?> GetReglaParaEditar(int reglaId)
        {
            return await _dbContext.ReglaModeracion.FirstOrDefaultAsync(r => r.ReglaId == reglaId);
        }

        public void AgregarRegla(ReglaModeracion regla)
        {
            _dbContext.ReglaModeracion.Add(regla);
        }

        // ---------------------------- Guardar ----------------------------

        // Un solo SaveChanges: el cambio y su registro de auditoría se guardan juntos o no se guarda nada
        public async Task<bool> GuardarCambios()
        {
            try
            {
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }
    }
}
