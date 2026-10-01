using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class ReporteEvento
{
    public int ReporteId { get; set; }

    public int EventoId { get; set; }

    public int? ReportadoPorUsuarioId { get; set; }

    public string Origen { get; set; } = null!;

    public string Motivo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaReporte { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public int? ReglaId { get; set; }

    public int? ResueltoPorUsuarioId { get; set; }

    public string? Resolucion { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual ReglaModeracion? Regla { get; set; }

    public virtual Usuario? ReportadoPorUsuario { get; set; }

    public virtual Usuario? ResueltoPorUsuario { get; set; }
}
