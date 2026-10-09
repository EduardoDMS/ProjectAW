namespace ProjectAW.Modules.Guias.Entities
{
    public class DetalleGuia
    {
        public int IdGuiaDetalle { get; set; }
        public int IdGuia { get; set; }
        public int IdProducto { get; set; }
        public decimal CantidadEsperada { get; set; }

        //unidad de medida
        // lote

    }
}
