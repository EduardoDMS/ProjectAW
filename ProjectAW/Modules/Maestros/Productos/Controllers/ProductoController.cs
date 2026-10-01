using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Maestros.Productos.Controllers
{
    [Route("Maestros/Producto")]
    public class ProductoController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            RouteData.Values["viewModule"] = "Maestros/Productos";
            return View();
        }
    }
}
