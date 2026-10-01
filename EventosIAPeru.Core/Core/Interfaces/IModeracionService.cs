using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>
    /// Reglas de negocio de US-15 (moderación). Todo es solo para ADMINISTRADOR,
    /// excepto ReportarEvento, que puede hacerlo cualquier usuario activo.
    /// </summary>
    public interface IModeracionService
    {
        // Panel con indicadores globales
        Task<(ResultadoOperacion Resultado, PanelModeracionDTO? Panel)> GetPanel(string firebaseUid);

        // Eventos
        Task<(ResultadoOperacion Resultado, ModeracionEventosDTO? Pagina)> BuscarEventos(string firebaseUid, string? estadoModeracion, string? texto, int pagina);
        Task<(ResultadoOperacion Resultado, DetalleEventoModeracionDTO? Detalle)> GetDetalleEvento(string firebaseUid, int eventoId);
        Task<ResultadoOperacion> ModerarEvento(string firebaseUid, int eventoId, ModerarEventoDTO dto);

        // Reportes
        Task<ResultadoOperacion> ReportarEvento(string firebaseUid, int eventoId, CrearReporteDTO dto);
        Task<(ResultadoOperacion Resultado, List<ReporteDTO>? Reportes)> GetReportes(string firebaseUid, string? estado);
        Task<ResultadoOperacion> ResolverReporte(string firebaseUid, int reporteId, ResolverReporteDTO dto);

        // Usuarios
        Task<(ResultadoOperacion Resultado, List<UsuarioDTO>? Usuarios)> BuscarUsuarios(string firebaseUid, string? texto, string? estado);
        Task<ResultadoOperacion> CambiarEstadoUsuario(string firebaseUid, int usuarioId, CambiarEstadoUsuarioDTO dto);
        Task<ResultadoOperacion> VerificarUsuario(string firebaseUid, int usuarioId);

        // Auditoría
        Task<(ResultadoOperacion Resultado, List<AuditoriaDTO>? Registros)> GetAuditoria(string firebaseUid, DateTime? desde, DateTime? hasta);
        Task<(ResultadoOperacion Resultado, byte[]? Archivo)> ExportarAuditoria(string firebaseUid);

        // Reglas automáticas
        Task<(ResultadoOperacion Resultado, List<ReglaModeracionDTO>? Reglas)> GetReglas(string firebaseUid);
        Task<ResultadoOperacion> CrearRegla(string firebaseUid, CrearReglaDTO dto);
        Task<ResultadoOperacion> CambiarEstadoRegla(string firebaseUid, int reglaId, bool activa);
    }
}
