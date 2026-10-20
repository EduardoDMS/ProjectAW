using System.ComponentModel.DataAnnotations;

namespace ProjectAW.Modules.Stocks.DTOs.Requests;

/// <summary>Una captura confirmada; no modifica inventario hasta cerrar la guía.</summary>
public sealed class RegistrarLecturaRequest
{
    [Range(1, int.MaxValue)] public int IdProducto { get; init; }
    [Range(1, int.MaxValue)] public int Cantidad { get; init; }
    [Required, StringLength(100)] public string CodigoLeido { get; init; } = string.Empty;
    [StringLength(100)] public string? Lote { get; init; }
    [StringLength(100)] public string? Serie { get; init; }
    [StringLength(300)] public string? Observacion { get; init; }
    public int? IdUbicacionOrigen { get; init; }
    public int? IdUbicacionDestino { get; init; }
}
