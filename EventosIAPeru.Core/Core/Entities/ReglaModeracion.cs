using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class ReglaModeracion
{
    public int ReglaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Criterio { get; set; } = null!;

    public string Accion { get; set; } = null!;

    public bool Activa { get; set; }

    public int? CreadaPorUsuarioId { get; set; }

    public string? Campo { get; set; }

    public string? Operador { get; set; }

    public decimal? Valor { get; set; }

    public virtual ICollection<Auditoria> Auditoria { get; set; } = new List<Auditoria>();

    public virtual Usuario? CreadaPorUsuario { get; set; }

    public virtual ICollection<ReporteEvento> ReporteEvento { get; set; } = new List<ReporteEvento>();
}
