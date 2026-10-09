namespace ProjectAW.Modules.Maestros.Estados.Entities
{
    public class EstadoGuia
    {
        public int IdEstadoGuia { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public static class EstadoGuiasIds
    {
        public const int Pendiente = 1;
        public const int EnProceso = 2;
        public const int Atendida = 3;
        public const int Cancelada = 4;
    }
}
