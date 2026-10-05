namespace ProjectAW.Modules.Maestros.Categorias.DTOs.Requests
{
    public class CrearCategoriaDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}
