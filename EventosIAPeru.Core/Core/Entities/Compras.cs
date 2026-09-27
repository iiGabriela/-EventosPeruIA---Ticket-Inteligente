using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Compras
{
    public int CompraId { get; set; }

    public int AsistenteId { get; set; }

    public int EventoId { get; set; }

    public string TitularNombre { get; set; } = null!;

    public string TitularDocumento { get; set; } = null!;

    public string MetodoPago { get; set; } = null!;

    public int CantidadEntradas { get; set; }

    public decimal MontoTotal { get; set; }

    public DateTime FechaCompra { get; set; }

    public virtual Usuarios Asistente { get; set; } = null!;

    public virtual ICollection<Entradas> Entradas { get; set; } = new List<Entradas>();

    public virtual Eventos Evento { get; set; } = null!;
}
