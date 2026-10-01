using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs
{
    // DTO: lo que manda el asistente al calificar un evento (US-13)
    public class CrearResenaDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Debes indicar el evento.")]
        public int EventoId { get; set; }

        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
        public int Calificacion { get; set; }

        [StringLength(1000, ErrorMessage = "El comentario no puede superar los 1000 caracteres.")]
        public string? Comentario { get; set; }
    }

    // DTO: una reseña para mostrar
    public class ResenaDTO
    {
        public int ResenaId { get; set; }
        public int EventoId { get; set; }
        public string? EventoNombre { get; set; }
        public string? UsuarioNombre { get; set; }
        public int Calificacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    // DTO: promedio y lista de reseñas de un evento
    public class ResumenResenasDTO
    {
        public int EventoId { get; set; }
        public double Promedio { get; set; }
        public int Total { get; set; }
        public List<ResenaDTO> Resenas { get; set; } = new List<ResenaDTO>();
    }

    // DTO: un evento al que el usuario asistió y todavía no reseña
    public class EventoPendienteResenaDTO
    {
        public int EventoId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Sede { get; set; } = null!;
        public DateTime FechaFin { get; set; }
    }

    // DTO: pestaña "Historial & Reseñas" (contador + lista de pendientes)
    public class PendientesResenaDTO
    {
        public int Total { get; set; }
        public List<EventoPendienteResenaDTO> Eventos { get; set; } = new List<EventoPendienteResenaDTO>();
    }
}
