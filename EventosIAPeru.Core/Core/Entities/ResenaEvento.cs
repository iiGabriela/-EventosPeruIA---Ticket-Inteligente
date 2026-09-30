using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class ResenaEvento
{
    public int ResenaId { get; set; }

    public int UsuarioId { get; set; }

    public int EventoId { get; set; }

    public short Calificacion { get; set; }

    public string? Comentario { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
