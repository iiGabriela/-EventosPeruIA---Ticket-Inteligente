using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class LogAuditoria
{
    public int LogId { get; set; }

    public int AdminId { get; set; }

    public string Accion { get; set; } = null!;

    public string EntidadAfectada { get; set; } = null!;

    public int EntidadId { get; set; }

    public string? Detalle { get; set; }

    public DateTime FechaAccion { get; set; }

    public virtual Usuarios Admin { get; set; } = null!;
}
