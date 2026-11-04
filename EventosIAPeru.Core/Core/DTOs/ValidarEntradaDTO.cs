using System.ComponentModel.DataAnnotations;

namespace EventosIAPeru.Core.Core.DTOs;

/// <summary>
/// US-08: se valida escaneando el QR (CodigoQr = token firmado) o, si la cámara falla,
/// ingresando a mano el ID de la entrada junto con el documento del titular.
/// </summary>
public class ValidarEntradaDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Indica el evento que estás validando.")]
    public int EventoId { get; set; }

    public string? CodigoQr { get; set; }

    public int? EntradaId { get; set; }

    public string? TitularDocumento { get; set; }
}
