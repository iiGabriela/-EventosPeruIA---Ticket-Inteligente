using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories;

public class NotificacionRepository : INotificacionRepository
{
    private readonly EventosPeruIAContext _dbContext;

    public NotificacionRepository(EventosPeruIAContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Notificacion>> GetNotificacionesDeUsuario(int usuarioId)
    {
        return await _dbContext.Notificacion
            .AsNoTracking()
            .Include(n => n.Evento)
            .Where(n => n.UsuarioId == usuarioId)
            .OrderBy(n => n.Leida)
            .ThenByDescending(n => n.FechaCreacion)
            .ToListAsync();
    }

    public async Task<Notificacion?> GetNotificacionDeUsuario(int usuarioId, int notificacionId)
    {
        return await _dbContext.Notificacion
            .FirstOrDefaultAsync(n => n.NotificacionId == notificacionId && n.UsuarioId == usuarioId);
    }

    public Task<bool> ExisteNotificacion(int usuarioId, int eventoId, string titulo)
    {
        return _dbContext.Notificacion
            .AsNoTracking()
            .AnyAsync(n => n.UsuarioId == usuarioId && n.EventoId == eventoId && n.Titulo == titulo);
    }

    public async Task<List<Compra>> GetComprasDeEventosProximos(DateTime desde, DateTime hasta, int? usuarioId = null)
    {
        var query = _dbContext.Compra
            .AsNoTracking()
            .Include(c => c.Evento)
            .Where(c => c.Estado == "CONFIRMADA"
                     && c.Evento.Estado == "PUBLICADO"
                     && c.Evento.FechaInicio > desde
                     && c.Evento.FechaInicio <= hasta);

        if (usuarioId.HasValue)
            query = query.Where(c => c.UsuarioId == usuarioId.Value);

        return await query.ToListAsync();
    }

    public async Task<List<int>> GetUsuariosConCompraConfirmada(int eventoId)
    {
        return await _dbContext.Compra
            .AsNoTracking()
            .Where(c => c.EventoId == eventoId && c.Estado == "CONFIRMADA")
            .Select(c => c.UsuarioId)
            .Distinct()
            .ToListAsync();
    }

    public async Task CrearNotificaciones(IEnumerable<Notificacion> notificaciones)
    {
        var lista = notificaciones.ToList();
        if (lista.Count == 0)
            return;

        await _dbContext.Notificacion.AddRangeAsync(lista);
        await _dbContext.SaveChangesAsync();
    }

    public async Task MarcarComoLeida(Notificacion notificacion)
    {
        notificacion.Leida = true;
        await _dbContext.SaveChangesAsync();
    }

    public async Task MarcarTodasComoLeidas(int usuarioId)
    {
        var notificaciones = await _dbContext.Notificacion
            .Where(n => n.UsuarioId == usuarioId && !n.Leida)
            .ToListAsync();

        foreach (var notificacion in notificaciones)
            notificacion.Leida = true;

        if (notificaciones.Count > 0)
            await _dbContext.SaveChangesAsync();
    }
}
