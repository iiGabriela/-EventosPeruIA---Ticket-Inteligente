using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class ReglasModeracionIa
{
    public int ReglaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string AccionAutomatica { get; set; } = null!;

    public bool Activa { get; set; }

    public DateTime FechaCreacion { get; set; }
}
