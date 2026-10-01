using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class CategoriaEvento
{
    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public long TotalConsultas { get; set; }

    public virtual ICollection<Evento> Evento { get; set; } = new List<Evento>();

    public virtual ICollection<Usuario> Usuario { get; set; } = new List<Usuario>();
}
