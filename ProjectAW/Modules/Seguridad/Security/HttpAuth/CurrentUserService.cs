using System.Security.Claims;

namespace ProjectAW.Modules.Seguridad.Security.HttpAuth
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _http;

        public CurrentUserService(IHttpContextAccessor http) => _http = http;

        public int IdUsuario => LeerClaimEntero(ClaimTypes.NameIdentifier);
        public int IdRol => LeerClaimEntero(AppClaimTypes.IdRol);

        private int LeerClaimEntero(string tipo)
        {
            var valor = _http.HttpContext?.User.FindFirst(tipo)?.Value;

            return int.TryParse(valor, out var id) ? id : throw new InvalidOperationException("No hay usuario autenticado en la sesion");
        }
    }
}
