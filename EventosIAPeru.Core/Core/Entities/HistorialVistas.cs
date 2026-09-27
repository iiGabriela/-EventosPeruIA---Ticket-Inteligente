using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class HistorialVistas
{
    public int VistaId { get; set; }

    public int UsuarioId { get; set; }

    public int EventoId { get; set; }

    public int CategoriaId { get; set; }

    public DateTime FechaVista { get; set; }

    public virtual Categorias Categoria { get; set; } = null!;

    public virtual Eventos Evento { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
