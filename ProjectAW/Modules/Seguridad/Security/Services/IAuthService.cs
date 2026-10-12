using Microsoft.AspNetCore.Identity.Data;
using ProjectAW.Modules.Seguridad.Security.DTOs.Request;
using ProjectAW.Modules.Seguridad.Security.DTOs.Response;

namespace ProjectAW.Modules.Seguridad.Security.Services
{
    public interface IAuthService
    {
        Task<SesionResponse?> ValidarCredencialAsync(Modules.Seguridad.Security.DTOs.Request.LoginRequest request, CancellationToken ct = default); 
    }
}
