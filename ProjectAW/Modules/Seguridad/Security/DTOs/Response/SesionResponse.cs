namespace ProjectAW.Modules.Seguridad.Security.DTOs.Response
{
    public record SesionResponse(int IdUsuario, string Username, string NombreCompleto, int IdRol);
}
