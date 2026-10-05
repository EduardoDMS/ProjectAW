namespace ProjectAW.Modules.Maestros.Ubicaciones.Entities
{
    public class Ubicacion
    {
        public int IdUbicacion { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FchRegistro { get; set; } = DateTime.Now;
        public DateTime? FchModificacion { get; set; }

    }
}
