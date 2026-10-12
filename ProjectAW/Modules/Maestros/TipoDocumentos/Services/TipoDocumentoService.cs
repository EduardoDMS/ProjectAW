using ProjectAW.Modules.Maestros.TipoDocumentos.DTOs.Requests;
using ProjectAW.Modules.Maestros.TipoDocumentos.DTOs.Response;
using ProjectAW.Modules.Maestros.TipoDocumentos.Entities;
using ProjectAW.Modules.Maestros.TipoDocumentos.Repositories;
using ProjectAW.Modules.Maestros.Ubicaciones.DTOs.Responses;

namespace ProjectAW.Modules.Maestros.TipoDocumentos.Services
{
    public class TipoDocumentoService : ITipoDocumentoService
    {
        private readonly ITipoDocumentoRepository _repository;

        public TipoDocumentoService(ITipoDocumentoRepository repository)
        {
            _repository = repository;
        }

        public async Task<TipoDocumentoDto> CrearTipoDocumento(CrearTipoDocumentoDto request)
        {
            var nombre = request.Documento.Trim();

            // falta logica para validar si ya existe uno con ese nombre 

            var documento = new TipoDocumento
            {
                Documento = nombre
            };

            await _repository.CrearAsync(documento);

            return new TipoDocumentoDto
            {
                IdTipoDoc = documento.IdTipoDocumento,
                Documento = documento.Documento
            };

        }

        public async Task<List<TipoDocumentoDto>> ObtenerTiposDocumentosAsync()
        {
            var tiposDocumentos = await _repository.GetAllAsync();

            return tiposDocumentos.Select( t => new TipoDocumentoDto
            {
                IdTipoDoc = t.IdTipoDocumento,
                Documento = t.Documento
            }).ToList();
        }
    }
}
