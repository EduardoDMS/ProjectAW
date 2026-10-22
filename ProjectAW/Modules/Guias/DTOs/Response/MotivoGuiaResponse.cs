namespace ProjectAW.Modules.Guias.DTOs.Response;

public sealed record MotivoGuiaResponse(
    int IdMotivoGuia, string NombreMotivo, int IdTipoOperacionGuia, string TipoOperacion,
    bool RequiereProveedor, bool RequiereCliente, bool RequiereAlmacenOrigen,
    bool RequiereAlmacenDestino, int IdTipoComportamiento, string NombreComportamiento,
    string DescripcionComportamiento, bool Activo);
