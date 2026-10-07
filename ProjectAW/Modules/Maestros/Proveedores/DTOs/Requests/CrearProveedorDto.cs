namespace ProjectAW.Modules.Maestros.Proveedores.DTOs.Requests
{
    public class CrearProveedorDto
    {
        public string RazonSocial { get; set; } = string.Empty;
        public int IdTipoDocumento { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? NombreContacto { get; set; }
        public string? CorreoContacto { get; set; }
        public string? TelefonoContacto { get; set; }
        public bool Activo { get; set; } = true;

        public DateTime FchRegistro { get; set; }=DateTime.Now;
    }
}
