namespace ProjectAW.Modules.Maestros.Almacenes.Entities
{
    public class Almacen
    {
        public int IdAlmacen { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FchRegistro { get; set; } = DateTime.Now;
        public DateTime? FchModificacion { get; set; }

        // conexion a la nueva tabla
        // public int CodigoUsuarioModificacion { get; set; }

    }
}
