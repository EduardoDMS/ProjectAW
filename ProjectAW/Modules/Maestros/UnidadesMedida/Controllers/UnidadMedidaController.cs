using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Maestros.UnidadesMedida.Controllers;

[Route("Maestros/UnidadesMedida")]
public class UnidadMedidaController : Controller
{
    [HttpGet("")]
    public IActionResult Index() { RouteData.Values["viewModule"] = "Maestros/UnidadesMedida"; return View(); }
}
