namespace ProjectAW.Modules.Maestros.Productos.Entities;

public class Producto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; }=string.Empty;
    public string? Descripcion { get; set; }
    public string? Lote { get; set; }
    public string Serie { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public DateTime FchRegistro { get; set; } = DateTime.Now;
    public DateTime? FchModificacion { get; set; }



    // conexion a nuevas tablas
    // public int CodigoUsuarioModificacion { get; set; } 

}
