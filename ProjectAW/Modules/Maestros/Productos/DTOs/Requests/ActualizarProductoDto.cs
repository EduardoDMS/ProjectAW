namespace ProjectAW.Modules.Maestros.Productos.DTOs.Requests
{
    public class ActualizarProductoDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Lote { get; set; }
        public string Serie { get; set; } = string.Empty;
        public bool Activo { get; set; }

        public DateTime FchModificacion { get; set; } = DateTime.Now;

        public int IdCategoria { get; set; }
    }
}
