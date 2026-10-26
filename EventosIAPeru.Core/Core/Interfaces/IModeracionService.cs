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
        Task<(ResultadoOperacion Resultado, PanelModeracionDTO? Panel)> GetPanel(string emailUsuario);

        // Eventos
        Task<(ResultadoOperacion Resultado, ModeracionEventosDTO? Pagina)> BuscarEventos(string emailUsuario, string? estadoModeracion, string? texto, int pagina);
        Task<(ResultadoOperacion Resultado, DetalleEventoModeracionDTO? Detalle)> GetDetalleEvento(string emailUsuario, int eventoId);
        Task<ResultadoOperacion> ModerarEvento(string emailUsuario, int eventoId, ModerarEventoDTO dto);

        // Reportes
        Task<ResultadoOperacion> ReportarEvento(string emailUsuario, int eventoId, CrearReporteDTO dto);
        Task<(ResultadoOperacion Resultado, List<ReporteDTO>? Reportes)> GetReportes(string emailUsuario, string? estado);
        Task<ResultadoOperacion> ResolverReporte(string emailUsuario, int reporteId, ResolverReporteDTO dto);

        // Usuarios
        Task<(ResultadoOperacion Resultado, List<UsuarioDTO>? Usuarios)> BuscarUsuarios(string emailUsuario, string? texto, string? estado);
        Task<ResultadoOperacion> CambiarEstadoUsuario(string emailUsuario, int usuarioId, CambiarEstadoUsuarioDTO dto);
        Task<ResultadoOperacion> VerificarUsuario(string emailUsuario, int usuarioId);

        // Auditoría
        Task<(ResultadoOperacion Resultado, List<AuditoriaDTO>? Registros)> GetAuditoria(string emailUsuario, DateTime? desde, DateTime? hasta);
        Task<(ResultadoOperacion Resultado, byte[]? Archivo)> ExportarAuditoria(string emailUsuario);

        // Reglas automáticas
        Task<(ResultadoOperacion Resultado, List<ReglaModeracionDTO>? Reglas)> GetReglas(string emailUsuario);
        Task<ResultadoOperacion> CrearRegla(string emailUsuario, CrearReglaDTO dto);
        Task<ResultadoOperacion> CambiarEstadoRegla(string emailUsuario, int reglaId, bool activa);
    }
}
