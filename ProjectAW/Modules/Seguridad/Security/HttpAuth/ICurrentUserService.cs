namespace ProjectAW.Modules.Seguridad.Security.HttpAuth
{
    public interface ICurrentUserService
    {
        int IdUsuario { get; }
        int IdRol { get; }
    }
}
