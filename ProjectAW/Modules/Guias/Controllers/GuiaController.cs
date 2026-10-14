using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Guias.Controllers;

[Route("Guias")]
public sealed class GuiaController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        RouteData.Values["viewModule"] = "Guias";
        ViewData["OpenCreate"] = false;
        return View("Index");
    }

    [HttpGet("Nueva")]
    public IActionResult Nueva()
    {
        RouteData.Values["viewModule"] = "Guias";
        ViewData["OpenCreate"] = true;
        return View("Index");
    }
}
