using ProjectAW.Modules.Maestros.Categorias.Entities;

namespace ProjectAW.Modules.Maestros.Categorias.Repositories
{
    public interface ICategoriaRepository
    {
        Task<List<Categoria>> GetAllAsync();
        Task<Categoria?> GetByIdAsync(int id);
        Task<Categoria?> GetByNombreAsync(string nombre);
        Task CreateAsync(Categoria categoria);
        Task UpdateAsync(Categoria categoria);

    }
}
