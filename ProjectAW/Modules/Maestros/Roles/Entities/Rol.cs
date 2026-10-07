using ProjectAW.Modules.Maestros.Usuarios.Entitites;

namespace ProjectAW.Modules.Maestros.Roles.Entities
{
    public class Rol
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; } = string.Empty;
        // public string Abreviatura { get; set; } = string.Empty;

        public ICollection<Usuario>? Usuario { get; set; }
    }
}
