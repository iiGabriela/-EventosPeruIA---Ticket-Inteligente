using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services
{
    /// <summary>
    /// Service: aplica las reglas de US-01 (registro), US-02 (sesión) y US-03 (doble rol).
    /// </summary>
    public class UsuarioService : IUsuarioService
    {
        // Nombres de rol tal como están en la tabla "rol"
        private const string RolAsistente = "ASISTENTE";
        private const string RolOrganizador = "ORGANIZADOR";
        private const string RolAdministrador = "ADMINISTRADOR";

        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        // ---------------------------- US-01 ----------------------------

        public async Task<ResultadoOperacion> Registrar(string emailUsuario, RegistroUsuarioDTO dto)
        {
            // Limpiamos espacios y dejamos el correo en minúsculas
            var nombre = dto.Nombre.Trim();
            var email = emailUsuario.Trim().ToLowerInvariant();

            if (nombre.Length < 2)
                return ResultadoOperacion.Invalido("El nombre debe tener al menos 2 caracteres.");

            // El correo identifica la cuenta: no se puede registrar dos veces
            if (await _usuarioRepository.ExisteEmail(email))
                return ResultadoOperacion.Invalido("Esta cuenta ya está registrada.");

            var dni = dto.Dni.Trim();
            if (await _usuarioRepository.ExisteDni(dni))
                return ResultadoOperacion.Invalido("El DNI ya está registrado en otra cuenta.");

            var usuario = new Usuario
            {
                Nombre = nombre,
                Email = email,
                Dni = dni,
                Estado = "ACTIVO",
                Verificado = false,
                FechaRegistro = DateTime.UtcNow
            };

            // Todo usuario nuevo empieza como ASISTENTE
            var creado = await _usuarioRepository.CrearUsuario(usuario, RolAsistente);
            if (!creado)
                return ResultadoOperacion.Invalido("No se pudo registrar el usuario.");

            return ResultadoOperacion.Ok(usuario.UsuarioId);
        }

        // ---------------------------- US-02 ----------------------------

        public async Task<(ResultadoOperacion Resultado, UsuarioDTO? Usuario)> ObtenerPerfil(string emailUsuario)
        {
            var (usuario, error) = await ObtenerUsuarioActivo(emailUsuario);
            if (error != null) return (error, null);

            return (ResultadoOperacion.Ok(usuario!.UsuarioId), Mapear(usuario));
        }

        // ---------------------------- US-03 ----------------------------

        public async Task<ResultadoOperacion> ActivarRolOrganizador(string emailUsuario)
        {
            var (usuario, error) = await ObtenerUsuarioActivo(emailUsuario);
            if (error != null) return error;

            // Si ya lo tiene no pasa nada, no hace falta otra cuenta
            if (usuario!.Rol.Any(r => r.Nombre == RolOrganizador))
                return ResultadoOperacion.Ok(usuario.UsuarioId);

            var agregado = await _usuarioRepository.AgregarRol(usuario.UsuarioId, RolOrganizador);
            if (!agregado)
                return ResultadoOperacion.Invalido("No se pudo activar el rol de organizador.");

            return ResultadoOperacion.Ok(usuario.UsuarioId);
        }

        public async Task<(ResultadoOperacion Resultado, CambioRolDTO? Cambio)> CambiarRolActivo(string emailUsuario, string rol)
        {
            var (usuario, error) = await ObtenerUsuarioActivo(emailUsuario);
            if (error != null) return (error, null);

            var rolPedido = rol.Trim().ToUpperInvariant();
            var misRoles = usuario!.Rol.Select(r => r.Nombre).OrderBy(n => n).ToList();

            // Solo puede cambiar a un rol que su cuenta ya tenga
            // (así nadie se vuelve ADMINISTRADOR solo pidiéndolo)
            if (!misRoles.Contains(rolPedido))
                return (ResultadoOperacion.NoAutorizado("Tu cuenta no tiene el rol " + rolPedido + "."), null);

            var cambio = new CambioRolDTO
            {
                RolActivo = rolPedido,
                RolesDisponibles = misRoles,
                Mensaje = "Ahora actúas como " + NombreBonito(rolPedido) + "."
            };
            return (ResultadoOperacion.Ok(usuario.UsuarioId), cambio);
        }

        // ---------------------------- Ayudantes ----------------------------

        // Busca al usuario y revisa que exista y esté ACTIVO (un bloqueado no puede usar la cuenta)
        private async Task<(Usuario? Usuario, ResultadoOperacion? Error)> ObtenerUsuarioActivo(string emailUsuario)
        {
            var usuario = await _usuarioRepository.GetUsuarioConRoles(emailUsuario);
            if (usuario == null)
                return (null, ResultadoOperacion.NoEncontrado("Tu usuario no está registrado. Completa tu registro."));

            if (usuario.Estado != "ACTIVO")
                return (null, ResultadoOperacion.NoAutorizado("Tu cuenta está desactivada o bloqueada."));

            return (usuario, null);
        }

        // Convierte la entidad en DTO
        private static UsuarioDTO Mapear(Usuario u)
        {
            return new UsuarioDTO
            {
                UsuarioId = u.UsuarioId,
                Nombre = u.Nombre,
                Email = u.Email,
                Dni = u.Dni,
                Estado = u.Estado,
                Verificado = u.Verificado,
                FechaRegistro = u.FechaRegistro,
                Roles = u.Rol.Select(r => r.Nombre).OrderBy(n => n).ToList()
            };
        }

        // Nombre para mostrar en el toast del frontend
        private static string NombreBonito(string rol)
        {
            return rol switch
            {
                RolAsistente => "Asistente",
                RolOrganizador => "Organizador",
                RolAdministrador => "Admin",
                _ => rol
            };
        }
    }
}
