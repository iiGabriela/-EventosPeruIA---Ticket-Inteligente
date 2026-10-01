using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories
{
    /// <summary>
    /// Repository de usuarios (US-01, US-02, US-03).
    /// Solo consulta y guarda en la base de datos.
    /// </summary>
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly EventosPeruIAContext _dbContext;

        public UsuarioRepository(EventosPeruIAContext dbContext)
        {
            _dbContext = dbContext;
        }

       
        public async Task<Usuario?> GetUsuarioByFirebaseUid(string firebaseUid)
        {
            var usuario = await _dbContext
                                .Usuario
                                .AsNoTracking()
                                .Where(u => u.FirebaseUid == firebaseUid)
                                .FirstOrDefaultAsync();
            return usuario;
        }

        // Busca al usuario por su UID de Firebase e incluye sus roles
        public async Task<Usuario?> GetUsuarioConRoles(string firebaseUid)
        {
            return await _dbContext
                            .Usuario
                            .AsNoTracking()
                            .Include(u => u.Rol)
                            .Where(u => u.FirebaseUid == firebaseUid)
                            .FirstOrDefaultAsync();
        }

        // Compara el correo en minúsculas (la base también lo guarda normalizado)
        public async Task<bool> ExisteEmail(string email)
        {
            var emailNormalizado = email.Trim().ToLower();
            return await _dbContext
                            .Usuario
                            .AnyAsync(u => u.Email.ToLower() == emailNormalizado);
        }

        // Guarda el usuario y le asigna el rol inicial en la tabla usuario_rol
        public async Task<bool> CrearUsuario(Usuario usuario, string nombreRol)
        {
            var rol = await _dbContext.Rol.FirstOrDefaultAsync(r => r.Nombre == nombreRol);
            if (rol == null) return false;

            usuario.Rol.Add(rol);
            _dbContext.Usuario.Add(usuario);

            try
            {
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                // Por ejemplo: dos personas se registraron con el mismo correo al mismo tiempo
                return false;
            }
        }

        // Agrega un rol extra a un usuario existente
        public async Task<bool> AgregarRol(int usuarioId, string nombreRol)
        {
            var usuario = await _dbContext
                                    .Usuario
                                    .Include(u => u.Rol)
                                    .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
            var rol = await _dbContext.Rol.FirstOrDefaultAsync(r => r.Nombre == nombreRol);

            if (usuario == null || rol == null) return false;

            // Si ya lo tiene, no lo repetimos
            if (usuario.Rol.Any(r => r.RolId == rol.RolId)) return true;

            usuario.Rol.Add(rol);

            try
            {
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        // Pregunta directo a la base si el usuario tiene ese rol
        public async Task<bool> TieneRol(int usuarioId, string nombreRol)
        {
            return await _dbContext
                            .Usuario
                            .AnyAsync(u => u.UsuarioId == usuarioId && u.Rol.Any(r => r.Nombre == nombreRol));
        }

        public async Task<List<CategoriaEvento>> GetIntereses(int usuarioId)
        {
            return await _dbContext.Usuario
                                   .AsNoTracking()
                                   .Where(u => u.UsuarioId == usuarioId)
                                   .SelectMany(u => u.Categoria)
                                   .OrderBy(c => c.Nombre)
                                   .ToListAsync();
        }

        public async Task<bool> ActualizarIntereses(int usuarioId, IReadOnlyCollection<CategoriaEvento> categorias)
        {
            var usuario = await _dbContext.Usuario
                                           .Include(u => u.Categoria)
                                           .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
            if (usuario == null)
                return false;

            usuario.Categoria.Clear();
            foreach (var categoria in categorias)
                usuario.Categoria.Add(categoria);

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
