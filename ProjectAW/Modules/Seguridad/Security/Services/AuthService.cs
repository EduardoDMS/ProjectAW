using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using ProjectAW.Modules.Seguridad.Security.DTOs.Response;
using ProjectAW.Modules.Seguridad.Security.Repositories;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;
using ProjectAW.Modules.Seguridad.Security.DTOs.Request;

namespace ProjectAW.Modules.Seguridad.Security.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IPasswordHasher<Usuario> _hasher;

        public AuthService(IAuthRepository repo ,IPasswordHasher<Usuario> hasher)
        {
            _repo = repo;
            _hasher = hasher;
        }

        public async Task<SesionResponse?> ValidarCredencialAsync(Modules.Seguridad.Security.DTOs.Request.LoginRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return null;

            var usuario = await _repo.ObtenerPorUsernameAsync(request.Username.Trim(), ct);

            if (usuario is null || !usuario.Activo || string.IsNullOrWhiteSpace(usuario.Password))
                return null;

            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.Password, request.Password);
            
            if (resultado == PasswordVerificationResult.Failed)
                return null;

            return new SesionResponse(
                usuario.IdUsuario, usuario.Username, $"{usuario.Nombre} {usuario.Apellido}", usuario.IdRol,
                usuario.rol?.Nombre ?? string.Empty);
        }
    }
}
