using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Seguridad.Security.HttpAuth;
using ProjectAW.Modules.Seguridad.Security.Services;
using System.Security.Claims;

namespace ProjectAW.Modules.Seguridad.Security.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthApiController(IAuthService service) => _service = service;

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(Modules.Seguridad.Security.DTOs.Request.LoginRequest request, CancellationToken ct)
        {
            var sesion = await _service.ValidarCredencialAsync(request, ct);

            if(sesion is null)
            {
                return Problem(detail: "Usuario o Contraseña incorrectos. ",
                               statusCode: StatusCodes.Status401Unauthorized,
                               title: "Credenciales invalidas");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, sesion.IdUsuario.ToString()),
                new(ClaimTypes.Name, sesion.Username),
                new(AppClaimTypes.IdRol,sesion.IdRol.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Ok(sesion);
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }
    }
}
