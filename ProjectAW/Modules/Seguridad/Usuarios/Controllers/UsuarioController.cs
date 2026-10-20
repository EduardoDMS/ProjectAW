using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Seguridad.Security.HttpAuth;

namespace ProjectAW.Modules.Seguridad.Usuarios.Controllers;

[Route("Maestros/Usuario")]
[Authorize(AuthenticationSchemes = AuthCookieSchemes.Admin)]
public class UsuarioController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        RouteData.Values["viewModule"] = "Seguridad/Usuarios";
        return View();
    }
}
