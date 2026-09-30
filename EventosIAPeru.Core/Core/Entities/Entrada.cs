using System;
using System.Collections.Generic;

namespace EventosIAPeru.Core.Core.Entities;

public partial class Entrada
{
    public int EntradaId { get; set; }

    public int CompraId { get; set; }

    public string CodigoQr { get; set; } = null!;

    public string TitularNombre { get; set; } = null!;

    public string TitularDocumento { get; set; } = null!;

    public string TipoAcceso { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaGeneracion { get; set; }

    public int? AsistenteUsuarioId { get; set; }

    public virtual Usuario? AsistenteUsuario { get; set; }

    public virtual CheckIn? CheckIn { get; set; }

    public virtual Compra Compra { get; set; } = null!;
}
