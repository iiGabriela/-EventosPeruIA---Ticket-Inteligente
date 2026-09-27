using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Usuarios
{
    public int UsuarioId { get; set; }

    public string FirebaseUid { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Dni { get; set; } = null!;

    public bool EsActivo { get; set; }

    public bool EsVerificado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Compras> Compras { get; set; } = new List<Compras>();

    public virtual ICollection<Eventos> Eventos { get; set; } = new List<Eventos>();

    public virtual ICollection<HistorialVistas> HistorialVistas { get; set; } = new List<HistorialVistas>();

    public virtual ICollection<LogAuditoria> LogAuditoria { get; set; } = new List<LogAuditoria>();

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();

    public virtual ICollection<PreferenciasUsuario> PreferenciasUsuario { get; set; } = new List<PreferenciasUsuario>();

    public virtual ICollection<ReportesEventos> ReportesEventos { get; set; } = new List<ReportesEventos>();

    public virtual ICollection<ReportesUsuarios> ReportesUsuariosUsuarioReporta { get; set; } = new List<ReportesUsuarios>();

    public virtual ICollection<ReportesUsuarios> ReportesUsuariosUsuarioReportado { get; set; } = new List<ReportesUsuarios>();

    public virtual ICollection<Resenas> Resenas { get; set; } = new List<Resenas>();

    public virtual ICollection<UsuarioRoles> UsuarioRoles { get; set; } = new List<UsuarioRoles>();
}
