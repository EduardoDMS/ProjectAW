namespace ProjectAW.Modules.Maestros.Ubicaciones.DTOs.Responses
{
    public class UbicacionDto
    {
        public int IdUbicacion { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; } 
        public bool Activo { get; set; }
    }
}
