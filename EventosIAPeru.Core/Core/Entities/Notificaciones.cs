using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Notificaciones
{
    public int NotificacionId { get; set; }

    public int UsuarioId { get; set; }

    public int? EventoId { get; set; }

    public string Tipo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public bool Leida { get; set; }

    public virtual Eventos? Evento { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}
