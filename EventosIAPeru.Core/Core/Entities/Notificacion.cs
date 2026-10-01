using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Notificacion
{
    public int NotificacionId { get; set; }

    public int UsuarioId { get; set; }

    public int EventoId { get; set; }

    public string Titulo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string Prioridad { get; set; } = "INFORMATIVO";

    public bool Leida { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
