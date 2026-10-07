using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Maestros.Proveedores.Controllers
{
    [Route("Maestros/Proveedor")]
    public class ProveedorController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            RouteData.Values["viewModule"] = "Maestros/Proveedores";
            return View();
        }
    }
}
