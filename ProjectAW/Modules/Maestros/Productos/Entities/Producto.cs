namespace ProjectAW.Modules.Maestros.Productos.Entities;

public class Producto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; }=string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}
