using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Auditoria
{
    public long AuditoriaId { get; set; }

    public int? ActorUsuarioId { get; set; }

    public string Accion { get; set; } = null!;

    public string? Detalle { get; set; }

    public DateTime Fecha { get; set; }

    public int? EventoId { get; set; }

    public int? UsuarioAfectadoId { get; set; }

    public int? ReglaId { get; set; }

    public virtual Usuario? ActorUsuario { get; set; }

    public virtual Evento? Evento { get; set; }

    public virtual ReglaModeracion? Regla { get; set; }

    public virtual Usuario? UsuarioAfectado { get; set; }
}
