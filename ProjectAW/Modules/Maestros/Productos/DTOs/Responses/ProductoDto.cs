namespace ProjectAW.Modules.Maestros.Productos.DTOs.Responses;

public class ProductoDto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } 
}
