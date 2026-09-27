using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Resenas
{
    public int ResenaId { get; set; }

    public int AsistenteId { get; set; }

    public int EventoId { get; set; }

    public int Calificacion { get; set; }

    public string? Comentario { get; set; }

    public DateTime FechaResena { get; set; }

    public virtual Usuarios Asistente { get; set; } = null!;

    public virtual Eventos Evento { get; set; } = null!;
}
