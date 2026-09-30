using Microsoft.EntityFrameworkCore;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Infrastructure.Data;

namespace EventosIAPeru.Core.Infrastructure.Repositories
{
    /// <summary>US-01, US-02, US-03 (Gabriela) completará este repositorio.</summary>
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
    }
}
