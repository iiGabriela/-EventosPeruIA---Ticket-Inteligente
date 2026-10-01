using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class CheckIn
{
    public int CheckInId { get; set; }

    public int EntradaId { get; set; }

    public int ValidadoPorUsuarioId { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Entrada Entrada { get; set; } = null!;

    public virtual Usuario ValidadoPorUsuario { get; set; } = null!;
}
