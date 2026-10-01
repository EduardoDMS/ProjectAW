namespace ProjectAW.Modules.Maestros.Productos.DTOs.Requests
{
    public class ActualizarProductoDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
    }
}
