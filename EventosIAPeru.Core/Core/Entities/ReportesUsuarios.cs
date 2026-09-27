using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class ReportesUsuarios
{
    public int ReporteId { get; set; }

    public int UsuarioReportadoId { get; set; }

    public int? UsuarioReportaId { get; set; }

    public string Motivo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaReporte { get; set; }

    public virtual Usuarios? UsuarioReporta { get; set; }

    public virtual Usuarios UsuarioReportado { get; set; } = null!;
}
