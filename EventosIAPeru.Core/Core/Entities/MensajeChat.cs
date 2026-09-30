using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class MensajeChat
{
    public int MensajeId { get; set; }

    public int ConversacionId { get; set; }

    public string Rol { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public virtual ConversacionChat Conversacion { get; set; } = null!;
}
