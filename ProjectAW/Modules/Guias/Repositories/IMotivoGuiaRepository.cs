using ProjectAW.Modules.Guias.Entities;

namespace ProjectAW.Modules.Guias.Repositories;

public interface IMotivoGuiaRepository
{
    Task<IReadOnlyList<MotivoGuia>> ListarAsync(int? idOperacion, bool soloActivos, CancellationToken ct = default);
    Task<MotivoGuia?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TipoComportamientoGuia>> ListarComportamientosAsync(CancellationToken ct = default);
    Task<bool> ExisteOperacionAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteComportamientoAsync(int id, CancellationToken ct = default);
    Task<bool> NombreExisteAsync(int idOperacion, string nombre, int? excluirId = null, CancellationToken ct = default);
    Task<bool> EstaEnUsoAsync(int id, CancellationToken ct = default);
    Task AgregarAsync(MotivoGuia motivo, CancellationToken ct = default);
    void Eliminar(MotivoGuia motivo);
    Task GuardarAsync(CancellationToken ct = default);
}
