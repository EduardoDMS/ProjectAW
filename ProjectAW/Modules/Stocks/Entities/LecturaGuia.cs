using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Maestros.Productos.Entities;
using ProjectAW.Modules.Maestros.Ubicaciones.Entities;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;

namespace ProjectAW.Modules.Stocks.Entities;

/// <summary>Registro durable de una lectura individual realizada durante una guía.</summary>
public sealed class LecturaGuia
{
    public long IdLecturaGuia { get; set; }
    public int IdGuia { get; set; }
    public int IdProducto { get; set; }
    public int IdUsuarioOperador { get; set; }
    public int Cantidad { get; set; }
    public string CodigoLeido { get; set; } = string.Empty;
    public string? Lote { get; set; }
    public string? Serie { get; set; }
    public string? Observacion { get; set; }
    public int? IdUbicacionOrigen { get; set; }
    public int? IdUbicacionDestino { get; set; }
    public DateTime FechaRegistroUtc { get; set; }

    public CabeceraGuia? Guia { get; set; }
    public Producto? Producto { get; set; }
    public Usuario? UsuarioOperador { get; set; }
    public Ubicacion? UbicacionOrigen { get; set; }
    public Ubicacion? UbicacionDestino { get; set; }
}
