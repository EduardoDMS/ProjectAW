using ProjectAW.Modules.Maestros.TipoDocumentos.DTOs.Requests;
using ProjectAW.Modules.Maestros.TipoDocumentos.DTOs.Response;

namespace ProjectAW.Modules.Maestros.TipoDocumentos.Services
{
    public interface ITipoDocumentoService
    {
        Task<List<TipoDocumentoDto>> ObtenerTiposDocumentosAsync();
        Task<TipoDocumentoDto> CrearTipoDocumento(CrearTipoDocumentoDto request);
    }
}
