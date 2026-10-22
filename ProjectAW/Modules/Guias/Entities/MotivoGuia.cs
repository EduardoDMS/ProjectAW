using ProjectAW.Modules.Maestros.Operaciones.Entities;

namespace ProjectAW.Modules.Guias.Entities;

/// <summary>Motivo configurable que determina los datos solicitados al crear una guía.</summary>
public sealed class MotivoGuia
{
    public int IdMotivoGuia { get; set; }
    public string NombreMotivo { get; set; } = string.Empty;
    public int IdTipoOperacionGuia { get; set; }
    public bool RequiereProveedor { get; set; }
    public bool RequiereCliente { get; set; }
    public bool RequiereAlmacenOrigen { get; set; }
    public bool RequiereAlmacenDestino { get; set; }
    public int IdTipoComportamiento { get; set; }
    public bool Activo { get; set; } = true;
    public TipoOperacionGuia? TipoOperacionGuia { get; set; }
    public TipoComportamientoGuia? TipoComportamiento { get; set; }
    public ICollection<CabeceraGuia> Guias { get; set; } = new List<CabeceraGuia>();
}
