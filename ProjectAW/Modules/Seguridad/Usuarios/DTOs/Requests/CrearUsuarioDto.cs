namespace ProjectAW.Modules.Seguridad.Usuarios.DTOs.Requests
{
    public class CrearUsuarioDto
    {
        //  public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public int IdRol { get; set; }

        public DateTime? FechaRegistro { get; set; } = DateTime.Now;
    }
}
