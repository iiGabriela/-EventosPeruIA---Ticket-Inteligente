using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class ReportesEventos
{
    public int ReporteId { get; set; }

    public int EventoId { get; set; }

    public int? UsuarioReportaId { get; set; }

    public string Motivo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaReporte { get; set; }

    public virtual Eventos Evento { get; set; } = null!;

    public virtual Usuarios? UsuarioReporta { get; set; }
}
