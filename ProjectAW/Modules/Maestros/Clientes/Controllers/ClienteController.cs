using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Maestros.Clientes.Controllers
{
    [Route("Maestros/Cliente")]
    public class ClienteController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            RouteData.Values["viewModule"] = "Maestros/Clientes";
            return View();
        }
    }
}
