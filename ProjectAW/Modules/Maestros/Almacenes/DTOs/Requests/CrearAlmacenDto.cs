namespace ProjectAW.Modules.Maestros.Almacenes.DTOs.Requests
{
    public class CrearAlmacenDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FchRegistro { get; set; } = DateTime.Now;
    }
}
