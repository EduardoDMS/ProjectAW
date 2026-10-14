using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Maestros.TipoDocumentos.Controllers;

[Route("Maestros/TiposDocumento")]
public sealed class TipoDocumentoController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        RouteData.Values["viewModule"] = "Maestros/TipoDocumentos";
        return View();
    }
}
