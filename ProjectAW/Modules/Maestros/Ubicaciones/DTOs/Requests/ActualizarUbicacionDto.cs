namespace ProjectAW.Modules.Maestros.Ubicaciones.DTOs.Requests
{
    public class ActualizarUbicacionDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public DateTime FchModificacion { get; set; } = DateTime.Now; 
        public int IdAlmacen { get; set; }
    }
}
