using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs;

public class ActualizarInteresesDTO
{
    [MaxLength(10, ErrorMessage = "Puedes seleccionar como máximo 10 categorías.")]
    public List<int> CategoriaIds { get; set; } = new();
}

public class RecomendacionEventoDTO : EventoDTO
{
    public int PuntajeAfinidad { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public class RecomendacionesDTO
{
    public bool Personalizadas { get; set; }
    public string Criterio { get; set; } = string.Empty;
    public List<RecomendacionEventoDTO> Eventos { get; set; } = new();
}

public class RecomendacionPerfil
{
    public List<int> CategoriasInteres { get; set; } = new();
    public List<int> CategoriasCompradas { get; set; } = new();
    public List<int> EventosComprados { get; set; } = new();
}

public class RecomendacionEventoItem
{
    public int EventoId { get; set; }
    public int OrganizadorId { get; set; }
    public string? OrganizadorNombre { get; set; }
    public int CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Sede { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
    public int AforoTotal { get; set; }
    public int EntradasVendidas { get; set; }
    public int CuposDisponibles { get; set; }
    public decimal Precio { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string EstadoModeracion { get; set; } = string.Empty;
}
