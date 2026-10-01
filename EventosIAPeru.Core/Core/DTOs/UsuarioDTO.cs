namespace EventosIAPeru.Core.Core.DTOs
{
    // DTO: lo que devolvemos del usuario (así no mostramos la entidad completa).
    public class UsuarioDTO
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public bool Verificado { get; set; }
        public DateTime FechaRegistro { get; set; }

        // Roles de la cuenta: ASISTENTE, ORGANIZADOR, ADMINISTRADOR
        public List<string> Roles { get; set; } = new List<string>();
    }
}
