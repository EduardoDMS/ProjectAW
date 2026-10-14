using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Maestros.Almacenes.Controllers
{
    [Route("Maestros/Almacen")]
    public class AlmacenController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            RouteData.Values["viewModule"] = "Maestros/Almacenes";
            return View();
        }
    }
}
