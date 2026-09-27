using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Categorias
{
    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Activa { get; set; }

    public int? Orden { get; set; }

    public virtual ICollection<Eventos> Eventos { get; set; } = new List<Eventos>();

    public virtual ICollection<HistorialVistas> HistorialVistas { get; set; } = new List<HistorialVistas>();

    public virtual ICollection<PreferenciasUsuario> PreferenciasUsuario { get; set; } = new List<PreferenciasUsuario>();
}
