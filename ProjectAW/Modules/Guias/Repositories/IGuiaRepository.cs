using ProjectAW.Modules.Guias.Entities;

namespace ProjectAW.Modules.Guias.Repositories
{
    public interface IGuiaRepository
    {
        Task<CabeceraGuia?> ObtenerPorIdAsync(int idGuia, CancellationToken ct = default);
        Task<IReadOnlyList<CabeceraGuia>> ListarAsync(int? idTipoOperacionGuia, int? idEstadoGuia, CancellationToken ct = default);
        Task<string> ObtenerSiguienteNumeroAsync(int idTipoOperacionGuia, CancellationToken ct = default);
        Task<bool> ExisteAsync<T>(int id, CancellationToken ct = default) where T : class;
        Task AgregarAsync(CabeceraGuia guia, CancellationToken ct = default);
        Task GuardarCambiosAsync(CancellationToken ct = default);
    }
}
