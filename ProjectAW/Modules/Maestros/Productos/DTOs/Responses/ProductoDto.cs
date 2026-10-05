using System.ComponentModel.DataAnnotations;

namespace ProjectAW.Modules.Maestros.Productos.DTOs.Responses;

public class ProductoDto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Lote { get; set; }
    public string Serie { get; set; } = string.Empty;
    public bool Activo { get; set; } 
}
