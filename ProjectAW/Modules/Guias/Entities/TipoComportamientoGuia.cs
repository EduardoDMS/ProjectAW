namespace ProjectAW.Modules.Guias.Entities;

/// <summary>Catálogo de las reglas de inventario aplicables a un motivo.</summary>
public sealed class TipoComportamientoGuia
{
    public int IdTipoComportamiento { get; set; }
    public string NombreComportamiento { get; set; } = string.Empty;
    public string DescripcionComportamiento { get; set; } = string.Empty;
    public ICollection<MotivoGuia> Motivos { get; set; } = new List<MotivoGuia>();
}
