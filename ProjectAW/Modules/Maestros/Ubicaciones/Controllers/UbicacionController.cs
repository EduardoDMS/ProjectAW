using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Maestros.Ubicaciones.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UbicacionController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            RouteData.Values["viewModule"] = "Maestros/Ubicaciones";
            return View();
        }

    }
}
