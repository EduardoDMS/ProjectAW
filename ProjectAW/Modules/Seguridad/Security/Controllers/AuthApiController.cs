using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectAW.Modules.Seguridad.Security.HttpAuth;
using ProjectAW.Modules.Seguridad.Security.Services;
using System.Security.Claims;

namespace ProjectAW.Modules.Seguridad.Security.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [EnableCors("PwaLogin")]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _service;
        private readonly IConfiguration _configuration;

        public AuthApiController(IAuthService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(Modules.Seguridad.Security.DTOs.Request.LoginRequest request, CancellationToken ct)
        {
            var sesion = await _service.ValidarCredencialAsync(request, ct);

            if(sesion is null)
            {
                return Problem(detail: "Usuario o Contraseña incorrectos. ",
                               statusCode: StatusCodes.Status401Unauthorized,
                               title: "Credenciales invalidas");
            }

            var pwaOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            var scheme = AuthCookieSchemes.Resolver(HttpContext, pwaOrigins);
            if (scheme == AuthCookieSchemes.Rechazar)
                return Problem(detail: "El origen de esta solicitud no está autorizado.",
                    statusCode: StatusCodes.Status403Forbidden, title: "Origen no permitido");

            var isPwaRequest = scheme == AuthCookieSchemes.Operador;
            var isOperator = string.Equals(sesion.Rol.Trim(), "Operador", StringComparison.OrdinalIgnoreCase);
            if (isPwaRequest != isOperator)
            {
                return Problem(
                    detail: isPwaRequest
                        ? "La aplicación de operadores requiere una cuenta con rol Operador."
                        : "Las cuentas de Operador deben iniciar sesión desde la aplicación móvil.",
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Rol no permitido para esta aplicación");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, sesion.IdUsuario.ToString()),
                new(ClaimTypes.Name, sesion.Username),
                new(AppClaimTypes.IdRol,sesion.IdRol.ToString())
            };

            var identity = new ClaimsIdentity(claims, scheme);

            await HttpContext.SignInAsync(
                scheme, new ClaimsPrincipal(identity));
            Response.Cookies.Delete(AuthCookieSchemes.LegacyCookieName, new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                HttpOnly = true
            });

            return Ok(sesion);
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var scheme = GetCurrentClientScheme();
            if (scheme == AuthCookieSchemes.Rechazar)
                return Problem(detail: "El origen de esta solicitud no está autorizado.",
                    statusCode: StatusCodes.Status403Forbidden, title: "Origen no permitido");
            await HttpContext.SignOutAsync(scheme);
            Response.Cookies.Delete(AuthCookieSchemes.LegacyCookieName, new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                HttpOnly = true
            });
            return NoContent();
        }

        private string GetCurrentClientScheme()
        {
            var pwaOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            return AuthCookieSchemes.Resolver(HttpContext, pwaOrigins);
        }
    }
}
