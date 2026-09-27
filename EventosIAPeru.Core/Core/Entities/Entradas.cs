using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Entradas
{
    public int EntradaId { get; set; }

    public int CompraId { get; set; }

    public string TokenSecreto { get; set; } = null!;

    public string TipoAcceso { get; set; } = null!;

    public decimal PrecioPagado { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime? FechaValidacion { get; set; }

    public virtual Compras Compra { get; set; } = null!;
}
