using ProjectAW.Modules.Maestros.TipoDocumentos.Entities;

namespace ProjectAW.Modules.Maestros.Proveedores.Entities
{
    public class Proveedor
    {
        public int IdProveedor { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public int IdTipoDocumento { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? NombreContacto { get; set; }
        public string? CorreoContacto { get; set; }
        public string? TelefonoContacto { get; set; }      
        public bool Activo { get; set; }
        public DateTime FchRegistro { get; set; } = DateTime.Now;
        public DateTime? FchModificacion { get; set; }

        public TipoDocumento TipoDocumento { get; set; } = null!;
    }
}
