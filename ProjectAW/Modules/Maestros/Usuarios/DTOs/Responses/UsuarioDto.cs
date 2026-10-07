namespace ProjectAW.Modules.Maestros.Usuarios.DTOs.Responses
{
    public class UsuarioDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
      //  public string Password { get; set; } = string.Empty;
        public int IdRol { get; set; }
       // public string NombreRol { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
