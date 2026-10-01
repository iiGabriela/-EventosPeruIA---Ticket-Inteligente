using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services;

/// <summary>
/// US-10: recomendaciones explicables por afinidad de categorías.
/// Interés explícito = 5 puntos; compra confirmada reciente = 3 puntos.
/// </summary>
public class RecomendacionService : IRecomendacionService
{
    private const int CantidadPorDefecto = 6;
    private const int CantidadMaxima = 20;
    private const int MesesHistorialCompras = 6;
    private const int PesoInteres = 5;
    private const int PesoCompra = 3;

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRecomendacionRepository _recomendacionRepository;

    public RecomendacionService(IUsuarioRepository usuarioRepository,
                                IRecomendacionRepository recomendacionRepository)
    {
        _usuarioRepository = usuarioRepository;
        _recomendacionRepository = recomendacionRepository;
    }

    public async Task<(ResultadoOperacion Resultado, RecomendacionesDTO? Recomendaciones)> ObtenerRecomendaciones(
        string firebaseUid,
        int cantidad)
    {
        if (cantidad <= 0)
            cantidad = CantidadPorDefecto;

        if (cantidad > CantidadMaxima)
            return (ResultadoOperacion.Invalido($"La cantidad máxima de recomendaciones es {CantidadMaxima}."), null);

        var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
        if (usuario == null)
            return (ResultadoOperacion.NoEncontrado("Tu usuario no está registrado. Completa tu registro."), null);

        if (usuario.Estado != "ACTIVO")
            return (ResultadoOperacion.NoAutorizado("Tu cuenta está desactivada o bloqueada."), null);

        var perfil = await _recomendacionRepository.ObtenerPerfil(
            usuario.UsuarioId,
            DateTime.UtcNow.AddMonths(-MesesHistorialCompras));

        var categoriasPorInteres = perfil.CategoriasInteres.ToHashSet();
        var categoriasPorCompra = perfil.CategoriasCompradas.ToHashSet();
        var categoriasAfinidad = categoriasPorInteres.Union(categoriasPorCompra).ToList();
        var eventosComprados = perfil.EventosComprados.ToHashSet();

        var candidatosPersonalizados = await _recomendacionRepository.ObtenerEventosPorCategorias(
            categoriasAfinidad,
            Math.Min(cantidad * 5, 100));

        var personalizados = candidatosPersonalizados
            .Where(e => !eventosComprados.Contains(e.EventoId))
            .Select(e => new
            {
                Evento = e,
                Puntaje = (categoriasPorInteres.Contains(e.CategoriaId) ? PesoInteres : 0)
                        + (categoriasPorCompra.Contains(e.CategoriaId) ? PesoCompra : 0)
            })
            .OrderByDescending(e => e.Evento.CuposDisponibles > 0)
            .ThenByDescending(e => e.Puntaje)
            .ThenByDescending(e => e.Evento.EntradasVendidas)
            .ThenBy(e => e.Evento.FechaInicio)
            .Select(e => Mapear(e.Evento, e.Puntaje, ConstruirMotivo(categoriasPorInteres.Contains(e.Evento.CategoriaId), categoriasPorCompra.Contains(e.Evento.CategoriaId))))
            .Take(cantidad)
            .ToList();

        var idsSeleccionados = eventosComprados
            .Union(personalizados.Select(e => e.EventoId))
            .ToHashSet();

        var faltantes = cantidad - personalizados.Count;
        var populares = faltantes > 0
            ? await _recomendacionRepository.ObtenerEventosPopulares(faltantes * 3, idsSeleccionados)
            : new List<RecomendacionEventoItem>();

        var recomendaciones = personalizados
            .Concat(populares
                .Where(e => !idsSeleccionados.Contains(e.EventoId))
                .Take(faltantes)
                .Select(e => Mapear(e, 0, ConstruirMotivoPopular(e))))
            .ToList();

        var esPersonalizada = personalizados.Count > 0;
        var criterio = esPersonalizada
            ? "Sugerencias personalizadas según tus intereses y compras anteriores."
            : "Eventos populares y próximos para descubrir nuevas actividades.";

        return (ResultadoOperacion.Ok(usuario.UsuarioId), new RecomendacionesDTO
        {
            Personalizadas = esPersonalizada,
            Criterio = criterio,
            Eventos = recomendaciones
        });
    }

    private static string ConstruirMotivo(bool tieneInteres, bool tieneCompra)
    {
        if (tieneInteres && tieneCompra)
            return "Coincide con tus intereses y compras anteriores.";
        if (tieneInteres)
            return "Coincide con una categoría de tu interés.";
        if (tieneCompra)
            return "Basado en una categoría de tus compras anteriores.";
        return "Sugerencia para descubrir nuevos eventos.";
    }

    private static string ConstruirMotivoPopular(RecomendacionEventoItem evento)
    {
        return evento.CuposDisponibles > 0
            ? "Evento próximo con cupos disponibles."
            : "Evento próximo y popular.";
    }

    private static RecomendacionEventoDTO Mapear(RecomendacionEventoItem evento, int puntaje, string motivo)
    {
        var cupos = Math.Max(evento.CuposDisponibles, 0);
        var ahora = DateTime.UtcNow;

        return new RecomendacionEventoDTO
        {
            EventoId = evento.EventoId,
            OrganizadorId = evento.OrganizadorId,
            OrganizadorNombre = evento.OrganizadorNombre,
            CategoriaId = evento.CategoriaId,
            CategoriaNombre = evento.CategoriaNombre,
            Nombre = evento.Nombre,
            Descripcion = evento.Descripcion,
            ImagenUrl = evento.ImagenUrl,
            FechaInicio = evento.FechaInicio,
            FechaFin = evento.FechaFin,
            Sede = evento.Sede,
            Direccion = evento.Direccion,
            Departamento = evento.Departamento,
            Provincia = evento.Provincia,
            Distrito = evento.Distrito,
            AforoTotal = evento.AforoTotal,
            EntradasVendidas = evento.EntradasVendidas,
            CuposDisponibles = cupos,
            Precio = evento.Precio,
            Estado = evento.Estado,
            EstadoModeracion = evento.EstadoModeracion,
            Agotado = cupos == 0,
            Finalizado = evento.FechaFin <= ahora,
            DisponibleParaCompra = evento.Estado == "PUBLICADO"
                                  && evento.EstadoModeracion != "DESACTIVADO"
                                  && evento.FechaInicio > ahora
                                  && cupos > 0,
            PuntajeAfinidad = puntaje,
            Motivo = motivo
        };
    }
}
