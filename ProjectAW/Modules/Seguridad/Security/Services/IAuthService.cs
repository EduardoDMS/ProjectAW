using ProjectAW.Modules.Seguridad.Security.DTOs.Response;
using LoginRequest = ProjectAW.Modules.Seguridad.Security.DTOs.Request.LoginRequest;

namespace ProjectAW.Modules.Seguridad.Security.Services
{
    public interface IAuthService
    {
        Task<SesionResponse?> ValidarCredencialAsync(LoginRequest request, CancellationToken ct = default);
    }
}
