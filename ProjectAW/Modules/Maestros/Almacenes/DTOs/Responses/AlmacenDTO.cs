namespace ProjectAW.Modules.Maestros.Almacenes.DTOs.Responses
{
    public class AlmacenDTO
    {
        public int IdAlmacen { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
        public List<int> IdUbicacionesDespacho { get; set; } = [];

        public DateTime FchRegistro { get; set; }

    }
}
