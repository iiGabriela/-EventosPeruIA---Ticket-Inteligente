using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>Repository de moderación (US-15). Solo habla con la base de datos.</summary>
    public interface IModeracionRepository
    {
        // ---------- Panel ----------
        Task<(int UsuariosActivos, int EventosPublicados, int RevisionesPendientes, int CuentasSuspendidas, int ReportesPendientes, int RegistrosAuditoria)> GetIndicadores();

        // ---------- Eventos ----------
        Task<(List<Evento> Eventos, int Total)> BuscarEventos(string? estadoModeracion, string? texto, int pagina, int tamano);
        Task<Evento?> GetEventoDetalle(int eventoId);          // solo lectura
        Task<Evento?> GetEventoParaModerar(int eventoId);      // con seguimiento (se va a modificar)

        // ---------- Reportes ----------
        Task<List<ReporteEvento>> GetReportes(string? estado);
        Task<ReporteEvento?> GetReporteParaResolver(int reporteId);
        Task<bool> ExisteEvento(int eventoId);
        Task<bool> ExisteReportePendiente(int usuarioId, int eventoId);
        void AgregarReporte(ReporteEvento reporte);

        // ---------- Usuarios ----------
        Task<List<Usuario>> BuscarUsuarios(string? texto, string? estado, int max);
        Task<Usuario?> GetUsuarioParaModerar(int usuarioId);

        // ---------- Auditoría ----------
        Task<List<Auditoria>> GetAuditoria(DateTime? desde, DateTime? hasta, int max);
        void AgregarAuditoria(Auditoria auditoria);

        // ---------- Reglas ----------
        Task<List<ReglaModeracion>> GetReglas();
        Task<ReglaModeracion?> GetReglaParaEditar(int reglaId);
        void AgregarRegla(ReglaModeracion regla);

        // Guarda todo lo pendiente (cambio + auditoría juntos, en una sola transacción)
        Task<bool> GuardarCambios();
    }
}
