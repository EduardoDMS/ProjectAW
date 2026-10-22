using ProjectAW.Modules.Maestros.Almacenes.DTOs.Requests;
using ProjectAW.Modules.Maestros.Almacenes.DTOs.Responses;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Almacenes.Repositories;
using ProjectAW.Infrastructure.Errors;

namespace ProjectAW.Modules.Maestros.Almacenes.Services
{
    public class AlmacenService : IAlmacenService
    { 
        private readonly IAlmacenRepository _repository;

        public AlmacenService(IAlmacenRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> ActualizarAlmacenAsync(int id, ActualizarAlmacenDto dto)
        {
            var almacen = await _repository.ObtenerAlmacenPorIdAsync(id);

            if (almacen == null)
                return false;

            var codigo = dto.Codigo.Trim();

            var almacenMismoCodigo = await _repository.ObtenerAlmacenPorCodigoAsync(codigo);

            if (almacenMismoCodigo != null && almacenMismoCodigo.IdAlmacen != id)
            {
                throw new ConflictoNegocioException("El código del almacén ya existe.");
            }

            almacen.Codigo = codigo;
            almacen.Descripcion = dto.Descripcion?.Trim();
            almacen.Activo = dto.Activo;
            almacen.FchModificacion = DateTime.UtcNow;

            return await _repository.ActualizarAlmacenAsync(almacen, dto.IdUbicacionesDespacho ?? []);

        }

        public async Task<bool> CambiarEstadoAlmacenAsync(int id, bool activo)
        {
            var almacen = await _repository.ObtenerAlmacenPorIdAsync(id);

            if (almacen == null)
                return false;

            almacen.Activo = activo;
            almacen.FchModificacion = DateTime.UtcNow;
            await _repository.ActualizarAlmacenAsync(almacen);

            return true;
        }

        public async Task<AlmacenDTO> CrearAlmacenAsync(CrearAlmacenDto dto)
        {
            if (dto.IdUbicacionesDespacho?.Count > 0)
                throw new ReglaNegocioException("Primero crea el almacén y sus ubicaciones; después podrás asignar las ubicaciones de despacho al editarlo.");
            var codigo = dto.Codigo.Trim();
            var almacenExistente = await _repository.ObtenerAlmacenPorCodigoAsync(codigo);

            if(almacenExistente != null)
                throw new ConflictoNegocioException("El código del almacén ya existe.");

            var almacen = new Almacen
            {
                Codigo = codigo,
                Descripcion = dto.Descripcion?.Trim(),
                Activo = dto.Activo,
                FchRegistro = DateTime.UtcNow
            };

            await _repository.CrearAlmacenAsync(almacen);

            return new AlmacenDTO
            {
                IdAlmacen = almacen.IdAlmacen,
                Codigo = almacen.Codigo,
                Descripcion = almacen.Descripcion,
                Activo = almacen.Activo,
                FchRegistro = almacen.FchRegistro,
                IdUbicacionesDespacho = []
            };

        }

        public async Task<List<AlmacenDTO>> ObtenerAlmacenesTodosAsync()
        {
            var almacenes = await _repository.ObtenerAlmacenesAsync();
            
            return almacenes.Select(a => new AlmacenDTO
            {
                IdAlmacen = a.IdAlmacen,
                Codigo = a.Codigo,
                Descripcion = a.Descripcion,
                Activo = a.Activo,
                FchRegistro = a.FchRegistro,
                IdUbicacionesDespacho = a.UbicacionesDespacho.Select(u => u.IdUbicacion).ToList()
            }).ToList();
        }

        public async Task<AlmacenDTO?> ObtenerAlmacenPorCodigoAsync(string codigo)
        {
            var almacen = await _repository.ObtenerAlmacenPorCodigoAsync(codigo);

            if(almacen == null)
                return null;

            return new AlmacenDTO
            {
                IdAlmacen = almacen.IdAlmacen,
                Codigo = almacen.Codigo,
                Descripcion = almacen.Descripcion,
                Activo = almacen.Activo,
                FchRegistro = almacen.FchRegistro,
                IdUbicacionesDespacho = almacen.UbicacionesDespacho.Select(u => u.IdUbicacion).ToList()
            };
        }

        public async Task<AlmacenDTO?> ObtenerAlmacenPorIdAsync(int id)
        {
             var almacen = await _repository.ObtenerAlmacenPorIdAsync(id);
            
             if(almacen == null)
                return null;

             return new AlmacenDTO
             {
                 IdAlmacen = almacen.IdAlmacen,
                 Codigo = almacen.Codigo,
                 Descripcion = almacen.Descripcion,
                 Activo = almacen.Activo,
                 FchRegistro = almacen.FchRegistro,
                 IdUbicacionesDespacho = almacen.UbicacionesDespacho.Select(u => u.IdUbicacion).ToList()
             };
        }
    }
}
