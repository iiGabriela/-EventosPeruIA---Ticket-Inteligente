using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class PreferenciasUsuario
{
    public int PreferenciaId { get; set; }

    public int UsuarioId { get; set; }

    public int CategoriaId { get; set; }

    public virtual Categorias Categoria { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
