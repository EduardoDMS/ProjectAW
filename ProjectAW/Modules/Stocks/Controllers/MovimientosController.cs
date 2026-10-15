using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Stocks.Controllers;

[Route("Stocks/Movimientos")]
public sealed class MovimientosController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        RouteData.Values["viewModule"] = "Stocks";
        return View("Movimientos");
    }
}
