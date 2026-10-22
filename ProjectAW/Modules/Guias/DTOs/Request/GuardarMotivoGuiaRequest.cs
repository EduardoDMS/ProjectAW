using System.ComponentModel.DataAnnotations;

namespace ProjectAW.Modules.Guias.DTOs.Request;

public sealed class GuardarMotivoGuiaRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string NombreMotivo { get; set; } = string.Empty;
    [Range(1, 2)] public int IdTipoOperacionGuia { get; set; }
    public bool RequiereProveedor { get; set; }
    public bool RequiereCliente { get; set; }
    public bool RequiereAlmacenOrigen { get; set; }
    public bool RequiereAlmacenDestino { get; set; }
    [Range(1, int.MaxValue)] public int IdTipoComportamiento { get; set; }
    public bool Activo { get; set; } = true;
}
