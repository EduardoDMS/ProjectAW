using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Seguridad.Usuarios.Controllers
{
    [Route("Maestros/Usuario")]
    public class UsuarioController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            RouteData.Values["viewModule"] = "Views/Shared/Login";
            return View();
        }
    }
}
