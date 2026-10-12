using ProjectAW.Modules.Maestros.TipoDocumentos.Entities;

namespace ProjectAW.Modules.Maestros.TipoDocumentos.Repositories
{
    public interface ITipoDocumentoRepository
    {
        Task<List<TipoDocumento>> GetAllAsync();
        Task<TipoDocumento?> GetByIdAsync(int id);
        Task CrearAsync(TipoDocumento dc);
        Task ActualizarAsync(TipoDocumento dc);
    }
}
