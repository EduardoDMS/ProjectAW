namespace ProjectAW.Modules.Maestros.Clientes.DTOs.Requests
{
    public class CrearClienteDto
    {
        public string RazonSocial { get; set; } = string.Empty;
        public int IdTipoDocumento { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public DateTime FchRegistro { get; set; } = DateTime.Now;
    }
}
