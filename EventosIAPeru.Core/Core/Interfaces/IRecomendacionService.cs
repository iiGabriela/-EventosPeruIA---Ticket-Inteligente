using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Interfaces;

public interface IRecomendacionService
{
    Task<(ResultadoOperacion Resultado, RecomendacionesDTO? Recomendaciones)> ObtenerRecomendaciones(
        string firebaseUid,
        int cantidad);
}
