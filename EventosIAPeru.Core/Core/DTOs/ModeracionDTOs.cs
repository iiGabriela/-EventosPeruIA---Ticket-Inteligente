using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs
{
    // ------------------------- Panel -------------------------

    // DTO: indicadores globales del panel de moderación
    public class PanelModeracionDTO
    {
        public int UsuariosActivos { get; set; }
        public int EventosPublicados { get; set; }
        public int RevisionesPendientes { get; set; }   // eventos publicados esperando revisión
        public int CuentasSuspendidas { get; set; }     // bloqueadas o inactivas
        public int ReportesPendientes { get; set; }
        public int RegistrosAuditoria { get; set; }
        public string AuditoriaEstado { get; set; } = "Activo";
        public string? Encriptacion { get; set; }       // lo completa el controller (TLS de la conexión)
    }

    // ------------------------- Eventos -------------------------

    // DTO: un evento en la lista de moderación
    public class ModeracionEventoDTO
    {
        public int EventoId { get; set; }
        public string Nombre { get; set; } = null!;
        public string OrganizadorNombre { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string EstadoModeracion { get; set; } = null!;   // EN_REVISION, CONFORME, DESACTIVADO
        public string? MotivoModeracion { get; set; }
        public int AforoTotal { get; set; }
        public DateTime FechaInicio { get; set; }
        public int ReportesPendientes { get; set; }
    }

    // DTO: página de eventos para moderar
    public class ModeracionEventosDTO
    {
        public int Total { get; set; }
        public int Pagina { get; set; }
        public List<ModeracionEventoDTO> Eventos { get; set; } = new List<ModeracionEventoDTO>();
    }

    // DTO: detalle de un evento con validaciones, aforo y reportes
    public class DetalleEventoModeracionDTO
    {
        public int EventoId { get; set; }
        public string Nombre { get; set; } = null!;
        public string OrganizadorNombre { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string EstadoModeracion { get; set; } = null!;
        public string? MotivoModeracion { get; set; }
        public bool? ValidacionSunat { get; set; }
        public bool? ValidacionMunicipal { get; set; }
        public int AforoTotal { get; set; }
        public int ReportesActivos { get; set; }
        public List<ReporteDTO> Reportes { get; set; } = new List<ReporteDTO>();
    }

    // DTO: el admin aprueba o suspende un evento
    public class ModerarEventoDTO
    {
        // CONFORME, DESACTIVADO o EN_REVISION
        [Required(ErrorMessage = "Debes indicar el estado de moderación.")]
        public string EstadoModeracion { get; set; } = null!;

        [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
        public string? Motivo { get; set; }

        // El frontend lo manda en true después de que el admin confirma en el diálogo
        public bool Confirmado { get; set; }
    }

    // ------------------------- Reportes -------------------------

    // DTO: un usuario reporta un evento
    public class CrearReporteDTO
    {
        [Required(ErrorMessage = "El motivo es obligatorio.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
        public string Motivo { get; set; } = null!;
    }

    // DTO: un reporte para mostrar
    public class ReporteDTO
    {
        public int ReporteId { get; set; }
        public int EventoId { get; set; }
        public string? EventoNombre { get; set; }
        public string Origen { get; set; } = null!;     // USUARIO, IA, SISTEMA
        public string Motivo { get; set; } = null!;
        public string Estado { get; set; } = null!;     // PENDIENTE, RESUELTO, DESCARTADO
        public string? ReportadoPor { get; set; }
        public DateTime FechaReporte { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string? Resolucion { get; set; }
    }

    // DTO: el admin resuelve o descarta un reporte
    public class ResolverReporteDTO
    {
        // RESUELTO o DESCARTADO
        [Required(ErrorMessage = "Debes indicar el estado.")]
        public string Estado { get; set; } = null!;

        [Required(ErrorMessage = "La resolución es obligatoria.")]
        [StringLength(1000, ErrorMessage = "La resolución no puede superar los 1000 caracteres.")]
        public string Resolucion { get; set; } = null!;

        public bool Confirmado { get; set; }
    }

    // ------------------------- Usuarios -------------------------

    // DTO: el admin cambia el estado de una cuenta
    public class CambiarEstadoUsuarioDTO
    {
        // ACTIVO, BLOQUEADO o INACTIVO
        [Required(ErrorMessage = "Debes indicar el estado.")]
        public string Estado { get; set; } = null!;

        [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
        public string? Motivo { get; set; }

        public bool Confirmado { get; set; }
    }

    // ------------------------- Auditoría -------------------------

    // DTO: una fila del log de auditoría
    public class AuditoriaDTO
    {
        public long AuditoriaId { get; set; }
        public DateTime Fecha { get; set; }
        public string? Actor { get; set; }
        public string Accion { get; set; } = null!;
        public string? Detalle { get; set; }
        public int? EventoId { get; set; }
        public int? UsuarioAfectadoId { get; set; }
        public int? ReglaId { get; set; }
    }

    // ------------------------- Reglas -------------------------

    // DTO: una regla automática de moderación
    public class ReglaModeracionDTO
    {
        public int ReglaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Tipo { get; set; } = null!;       // SIMPLE o IA
        public string Criterio { get; set; } = null!;
        public string Accion { get; set; } = null!;     // EN_REVISION o DESACTIVADO
        public bool Activa { get; set; }
        public string? Campo { get; set; }              // AFORO_TOTAL o PRECIO (solo SIMPLE)
        public string? Operador { get; set; }           // MAYOR, MAYOR_IGUAL, MENOR, MENOR_IGUAL, IGUAL
        public decimal? Valor { get; set; }
    }

    // DTO: crear una regla. SIMPLE necesita campo, operador y valor. IA solo el criterio en texto.
    public class CrearReglaDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El tipo es obligatorio (SIMPLE o IA).")]
        public string Tipo { get; set; } = null!;

        [Required(ErrorMessage = "El criterio es obligatorio.")]
        public string Criterio { get; set; } = null!;

        [Required(ErrorMessage = "La acción es obligatoria (EN_REVISION o DESACTIVADO).")]
        public string Accion { get; set; } = null!;

        public string? Campo { get; set; }
        public string? Operador { get; set; }
        public decimal? Valor { get; set; }
    }
}
