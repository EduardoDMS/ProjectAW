namespace ProjectAW.Modules.Maestros.Operaciones.Entities
{
    public class TipoOperacionGuia
    {
        public int IdTipoOperacionGuia { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public static class TipoOperacionGuiaIds
    {
        public const int Entrada = 1;
        public const int Salida = 2;
        public const int Traslado = 3;
    }
}
