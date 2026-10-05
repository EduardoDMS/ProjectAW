using ProjectAW.Modules.Maestros.Categorias.DTOs.Requests;
using ProjectAW.Modules.Maestros.Categorias.DTOs.Responses;
using ProjectAW.Modules.Maestros.Categorias.Entities;
using ProjectAW.Modules.Maestros.Categorias.Repositories;

namespace ProjectAW.Modules.Maestros.Categorias.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _repository;
        public CategoriaService(ICategoriaRepository repository)
        {
            _repository = repository;
        }


        public async Task<bool> ActualizarCategoriaAsync(int id, ActualizarCategoriaDto dto)
        {
            var categoria = await _repository.GetByIdAsync(id);

            if (categoria == null)
                throw new InvalidOperationException($"No se encontró la categoría con ID {id}.");

            var nombreCate = dto.Nombre.Trim().ToUpper();
            var categoriaConMismoNombre = await _repository.GetByNombreAsync(nombreCate);

            if(categoriaConMismoNombre != null && categoriaConMismoNombre.IdCategoria != id)
            {
                throw new InvalidOperationException($"Ya existe una categoría con el nombre '{nombreCate}'.");
            }

            categoria.Nombre = nombreCate;
            categoria.Descripcion = dto.Descripcion;
            categoria.Activo = dto.Activo;

            await _repository.UpdateAsync(categoria);
            return true;
        }

        public async Task<bool> CambiarEstadoCategoriaAsync(int id, bool activo)
        {
            var categoria = await _repository.GetByIdAsync(id);

            if (categoria == null)
                return false;

            categoria.Activo = activo;
            await _repository.UpdateAsync(categoria);
            return true;
        }

        public async Task<CategoriaDto> CrearCategoriaAsync(CrearCategoriaDto dto)
        {
            var nombre = dto.Nombre.Trim().ToUpper();

            var categoriaExistente = await _repository.GetByNombreAsync(nombre);
            if(categoriaExistente != null)
                throw new InvalidOperationException($"Ya existe una categoría con el nombre '{nombre}'.");

            var categoria = new Categoria
            {
                Nombre = nombre,
                Descripcion = dto.Descripcion,
                Activo = dto.Activo
            };

            await _repository.CreateAsync(categoria);

            return new CategoriaDto
            {
                IdCategoria = categoria.IdCategoria,
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                Activo = categoria.Activo
            };

        }

        public async Task<List<CategoriaDto>> ListarCategoriasAsync()
        {
            var categorias = await _repository.GetAllAsync();

            return categorias.Select(c => new CategoriaDto
            {
                IdCategoria = c.IdCategoria,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                Activo = c.Activo
            }).ToList();

        }

        public async Task<CategoriaDto?> ObtenerCategoriaPorIdAsync(int id)
        {
            var categoria = await _repository.GetByIdAsync(id);
            if (categoria == null)
                return null;

            return new CategoriaDto
            {
                IdCategoria = categoria.IdCategoria,
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                Activo = categoria.Activo
            };
        }

        public async Task<CategoriaDto?> ObtenerCategoriaPorNombreAsync(string nombre)
        {
             var categoria = await _repository.GetByNombreAsync(nombre);
             if (categoria == null)
                 return null;

             return new CategoriaDto
             {
                 IdCategoria = categoria.IdCategoria,
                 Nombre = categoria.Nombre,
                 Descripcion = categoria.Descripcion,
                 Activo = categoria.Activo
             };
        }
    }
}
