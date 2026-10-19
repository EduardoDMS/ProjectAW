namespace ProjectAW.Modules.Seguridad.Security.HttpAuth;

public static class AuthCookieSchemes
{
    public const string Selector = "ProjectAW.Auth.Selector";
    public const string Admin = "ProjectAW.Auth.Admin";
    public const string Operador = "ProjectAW.Auth.Operador";
    public const string Rechazar = "ProjectAW.Auth.Reject";
    public const string LegacyCookieName = "ProjectAW.Auth";
    public const string AdminCookieName = "ProjectAW.Admin.Auth";
    public const string OperadorCookieName = "ProjectAW.Operador.Auth";
    public const string RechazarCookieName = "ProjectAW.Reject.Auth";

    public static string Resolver(HttpContext context, IReadOnlyCollection<string> pwaOrigins)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
            return Admin;
        if (pwaOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            return Operador;

        var apiOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
        return string.Equals(origin, apiOrigin, StringComparison.OrdinalIgnoreCase)
            ? Admin
            : Rechazar;
    }
}
