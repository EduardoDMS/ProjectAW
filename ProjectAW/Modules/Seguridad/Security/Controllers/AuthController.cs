using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Seguridad.Security.Controllers;

[AllowAnonymous]
public sealed class AuthController : Controller
{
    [HttpGet("/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "Home");

        return View("~/Modules/Seguridad/Security/Views/Login.cshtml");
    }

    [HttpGet("/AccesoDenegado")]
    public IActionResult AccessDenied() =>
        View("~/Modules/Seguridad/Security/Views/AccessDenied.cshtml");

    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl!)
            : RedirectToAction("Index", "Home");
}
