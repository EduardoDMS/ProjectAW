namespace ProjectAW.Modules.Maestros.Almacenes.DTOs.Requests
{
    public class ActualizarAlmacenDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public DateTime FchModificacion { get; set; } = DateTime.Now;
    }
}
