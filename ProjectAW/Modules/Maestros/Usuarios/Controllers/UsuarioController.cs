using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Maestros.Usuarios.Controllers
{
    [Route("Maestros/Usuario")]
    public class UsuarioController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            RouteData.Values["viewModule"] = "Maestros/Usuarios";
            return View();
        }
    }
}
