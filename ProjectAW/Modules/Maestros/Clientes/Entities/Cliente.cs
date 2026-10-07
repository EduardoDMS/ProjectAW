using ProjectAW.Modules.Maestros.TipoDocumentos.Entities;

namespace ProjectAW.Modules.Maestros.Clientes.Entities
{
    public class Cliente
    {
        public int IdCliente { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public int IdTipoDocumento { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime FchRegistro { get; set; } = DateTime.Now;
        public DateTime? FchModificacion { get; set; }

        public TipoDocumento TipoDocumento { get; set; } = null!;
    }
}
