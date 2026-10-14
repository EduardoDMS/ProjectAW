using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Maestros.Roles.Controllers;

[Route("Maestros/Roles")]
public sealed class RolController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        RouteData.Values["viewModule"] = "Maestros/Roles";
        return View();
    }
}
