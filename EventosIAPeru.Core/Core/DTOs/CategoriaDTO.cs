namespace EventosIAPeru.Core.Core.DTOs
{
    public class CategoriaDTO
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = null!;
        public long TotalConsultas { get; set; }
    }
}
