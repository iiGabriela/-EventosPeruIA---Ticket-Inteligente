using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Evento
{
    public int EventoId { get; set; }

    public int OrganizadorId { get; set; }

    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string? ImagenUrl { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string Sede { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string Departamento { get; set; } = null!;

    public string Provincia { get; set; } = null!;

    public string Distrito { get; set; } = null!;

    public int AforoTotal { get; set; }

    public decimal Precio { get; set; }

    public string Estado { get; set; } = null!;

    public string EstadoModeracion { get; set; } = null!;

    public string? MotivoModeracion { get; set; }

    public bool? ValidacionSunat { get; set; }

    public bool? ValidacionMunicipal { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public virtual ICollection<Auditoria> Auditoria { get; set; } = new List<Auditoria>();

    public virtual CategoriaEvento Categoria { get; set; } = null!;

    public virtual ICollection<Compra> Compra { get; set; } = new List<Compra>();

    public virtual ICollection<EventoEmbedding> EventoEmbedding { get; set; } = new List<EventoEmbedding>();

    public virtual ICollection<Notificacion> Notificacion { get; set; } = new List<Notificacion>();

    public virtual Usuario Organizador { get; set; } = null!;

    public virtual ICollection<ReporteEvento> ReporteEvento { get; set; } = new List<ReporteEvento>();

    public virtual ICollection<ResenaEvento> ResenaEvento { get; set; } = new List<ResenaEvento>();
}
