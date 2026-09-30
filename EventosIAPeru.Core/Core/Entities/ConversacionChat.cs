using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class ConversacionChat
{
    public int ConversacionId { get; set; }

    public int UsuarioId { get; set; }

    public DateTime FechaInicio { get; set; }

    public virtual ICollection<MensajeChat> MensajeChat { get; set; } = new List<MensajeChat>();

    public virtual Usuario Usuario { get; set; } = null!;
}
