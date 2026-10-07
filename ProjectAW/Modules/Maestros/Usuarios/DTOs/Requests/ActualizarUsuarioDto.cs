namespace ProjectAW.Modules.Maestros.Usuarios.DTOs.Requests
{
    public class ActualizarUsuarioDto
    {
       // public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;

        public DateTime FechaActualizacion { get; set; }

        public int IdRol { get; set; }
        public bool Activo { get; set; }
    }
}
