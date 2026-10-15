using System.ComponentModel.DataAnnotations;

namespace ProjectAW.Modules.Stocks.DTOs.Requests;

/// <summary>
/// Conjunto completo de cantidades contadas por el operador antes de cerrar la guía.
/// </summary>
public sealed class FinalizarLecturaRequest
{
    [Required, MinLength(1)]
    public List<LecturaProductoRequest> Lecturas { get; init; } = [];
}

public sealed class LecturaProductoRequest
{
    [Range(1, int.MaxValue)]
    public int IdProducto { get; init; }

    public int? IdUbicacionOrigen { get; init; }
    public int? IdUbicacionDestino { get; init; }

    // Cero representa que el producto esperado no se encontró durante el conteo.
    [Range(0, int.MaxValue)]
    public int CantidadLeida { get; init; }

    [StringLength(300)]
    public string? Observacion { get; init; }
}
