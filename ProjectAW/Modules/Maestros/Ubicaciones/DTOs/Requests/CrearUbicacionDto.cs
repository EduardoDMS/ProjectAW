namespace ProjectAW.Modules.Maestros.Ubicaciones.DTOs.Requests
{
    public class CrearUbicacionDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public DateTime FchRegistro { get; set; } = DateTime.Now;
    }
}
