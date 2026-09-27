using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class UsuarioRoles
{
    public int UsuarioId { get; set; }

    public int RolId { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public virtual Roles Rol { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
