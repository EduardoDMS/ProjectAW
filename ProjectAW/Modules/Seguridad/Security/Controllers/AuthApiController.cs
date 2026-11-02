using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectAW.Modules.Seguridad.Security.HttpAuth;
using ProjectAW.Modules.Seguridad.Security.Services;
using System.Security.Claims;
using LoginRequest = ProjectAW.Modules.Seguridad.Security.DTOs.Request.LoginRequest;
using SesionResponse = ProjectAW.Modules.Seguridad.Security.DTOs.Response.SesionResponse;

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

        /// <summary>
        /// Valida las credenciales, determina el esquema según el origen y el rol,
        /// y solo entonces emite la cookie del cliente correspondiente.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        {
            var sesion = await _service.ValidarCredencialAsync(request, ct);
            if (sesion is null)
                return Problem(detail: "Usuario o Contraseña incorrectos. ",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Credenciales invalidas");

            var scheme = AuthCookieSchemes.Resolver(HttpContext, ObtenerOrigenesPwa());
            if (scheme == AuthCookieSchemes.Rechazar)
                return Problem(detail: "El origen de esta solicitud no está autorizado.",
                    statusCode: StatusCodes.Status403Forbidden, title: "Origen no permitido");

            var respuestaRol = ValidarRolParaCliente(sesion, scheme);
            if (respuestaRol is not null)
                return respuestaRol;

            await IniciarSesionAsync(sesion, scheme);

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
            EliminarCookieAnterior();
            return NoContent();
        }

        private IActionResult? ValidarRolParaCliente(SesionResponse sesion, string scheme)
        {
            var esClienteOperador = scheme == AuthCookieSchemes.Operador;
            var esUsuarioOperador = string.Equals(sesion.Rol.Trim(), "Operador", StringComparison.OrdinalIgnoreCase);
            if (esClienteOperador == esUsuarioOperador)
                return null;

            return Problem(
                detail: esClienteOperador
                    ? "La aplicación de operadores requiere una cuenta con rol Operador."
                    : "Las cuentas de Operador deben iniciar sesión desde la aplicación móvil.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Rol no permitido para esta aplicación");
        }

        private async Task IniciarSesionAsync(SesionResponse sesion, string scheme)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, sesion.IdUsuario.ToString()),
                new(ClaimTypes.Name, sesion.Username),
                new(AppClaimTypes.IdRol, sesion.IdRol.ToString())
            };

            var identity = new ClaimsIdentity(claims, scheme);
            await HttpContext.SignInAsync(scheme, new ClaimsPrincipal(identity));
            EliminarCookieAnterior();
        }

        private void EliminarCookieAnterior() =>
            Response.Cookies.Delete(AuthCookieSchemes.LegacyCookieName, new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                HttpOnly = true
            });

        private string GetCurrentClientScheme()
        {
            return AuthCookieSchemes.Resolver(HttpContext, ObtenerOrigenesPwa());
        }

        private string[] ObtenerOrigenesPwa() =>
            _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    }
}
