using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    public string FirebaseUid { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public bool Verificado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Auditoria> AuditoriaActorUsuario { get; set; } = new List<Auditoria>();

    public virtual ICollection<Auditoria> AuditoriaUsuarioAfectado { get; set; } = new List<Auditoria>();

    public virtual ICollection<CheckIn> CheckIn { get; set; } = new List<CheckIn>();

    public virtual ICollection<Compra> Compra { get; set; } = new List<Compra>();

    public virtual ICollection<ConversacionChat> ConversacionChat { get; set; } = new List<ConversacionChat>();

    public virtual ICollection<Entrada> Entrada { get; set; } = new List<Entrada>();

    public virtual ICollection<Evento> Evento { get; set; } = new List<Evento>();

    public virtual ICollection<Notificacion> Notificacion { get; set; } = new List<Notificacion>();

    public virtual ICollection<ReglaModeracion> ReglaModeracion { get; set; } = new List<ReglaModeracion>();

    public virtual ICollection<ReporteEvento> ReporteEventoReportadoPorUsuario { get; set; } = new List<ReporteEvento>();

    public virtual ICollection<ReporteEvento> ReporteEventoResueltoPorUsuario { get; set; } = new List<ReporteEvento>();

    public virtual ICollection<ResenaEvento> ResenaEvento { get; set; } = new List<ResenaEvento>();

    public virtual ICollection<CategoriaEvento> Categoria { get; set; } = new List<CategoriaEvento>();

    public virtual ICollection<Rol> Rol { get; set; } = new List<Rol>();
}
