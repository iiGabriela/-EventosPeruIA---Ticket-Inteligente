using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Eventos
{
    public int EventoId { get; set; }

    public int OrganizadorId { get; set; }

    public int CategoriaId { get; set; }

    public string Titulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string ImagenUrl { get; set; } = null!;

    public DateTime FechaHora { get; set; }

    public string Distrito { get; set; } = null!;

    public string Ubicacion { get; set; } = null!;

    public int AforoMaximo { get; set; }

    public int CuposDisponibles { get; set; }

    public decimal Precio { get; set; }

    public string Estado { get; set; } = null!;

    public string EstadoModeracion { get; set; } = null!;

    public string? MotivoModeracion { get; set; }

    public bool ValidacionMunicipal { get; set; }

    public DateTime FechaCreacion { get; set; }

    public byte[]? EmbeddingData { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Categorias Categoria { get; set; } = null!;

    public virtual ICollection<Compras> Compras { get; set; } = new List<Compras>();

    public virtual ICollection<HistorialVistas> HistorialVistas { get; set; } = new List<HistorialVistas>();

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();

    public virtual Usuarios Organizador { get; set; } = null!;

    public virtual ICollection<ReportesEventos> ReportesEventos { get; set; } = new List<ReportesEventos>();

    public virtual ICollection<Resenas> Resenas { get; set; } = new List<Resenas>();
}
