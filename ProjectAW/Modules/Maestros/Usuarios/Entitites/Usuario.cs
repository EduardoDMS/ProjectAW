using ProjectAW.Modules.Maestros.Roles.Entities;

namespace ProjectAW.Modules.Maestros.Usuarios.Entitites
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime FechaActualizada { get; set; }
        public bool Activo { get; set; } 

        public int IdRol { get; set; }
        public Rol? rol { get; set; }

    }
}
