using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Productos.Entities;

namespace ProjectAW.Modules.Stocks.Entities
{
    public class StockAlmacen
    {
        public int IdStockAlmacen { get; set; }
        public int IdAlmacen { get; set; }
        public int IdProducto { get; set; }
        public int CantidadActual { get; set; }
        public DateTime FechaUltimoRegistro { get; set; } //= DateTime.Now;


        public Almacen? Almacen { get; set; }
        public Producto? Producto { get; set; }
        

    }
}
