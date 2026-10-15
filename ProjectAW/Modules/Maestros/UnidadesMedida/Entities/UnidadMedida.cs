using ProjectAW.Modules.Maestros.Productos.Entities;

namespace ProjectAW.Modules.Maestros.UnidadesMedida.Entities;

public class UnidadMedida
{
    public int IdUnidadMedida { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
