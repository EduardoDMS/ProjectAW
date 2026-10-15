using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Maestros.Productos.Entities;
using ProjectAW.Modules.Maestros.Ubicaciones.Entities;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;

namespace ProjectAW.Modules.Stocks.Entities
{
    public class MovimientoStock
    {
        public int IdMovimientoStock { get; set; }
        public int IdProducto { get; set; }
        public int IdAlmacen { get; set; }
        public int IdTipoOperacion { get; set; }
        public int Cantidad { get; set; }
        public DateTime FchMovimiento { get; set; } = DateTime.Now;
    
        public int IdGuia { get; set; }
        public int IdUsuarioOperador { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public int CantidadDiferenciada { get; set; } 
        public int? IdUbicacionOrigen { get; set; }
        public int? IdUbicacionDestino { get; set; }

        public Producto? Producto { get; set; }
        public Almacen? Almacen { get; set; }
        public TipoOperacionGuia? OperacionGuias { get; set; }
        public CabeceraGuia? Guia { get; set; }
        public Usuario? Usuario { get; set; } // tecnicamente solo reconocera a los usuarios que sean de rol operador :v pero aun estamos probando todo y faltaria crear ese proyecto asi que por ahora simulamos con un api 
        public Ubicacion? UbicacionOrigen { get; set; }
        public Ubicacion? UbicacionDestino { get; set; }
    }
}
