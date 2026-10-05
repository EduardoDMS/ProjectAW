using ProjectAW.Modules.Maestros.Ubicaciones.DTOs.Requests;
using ProjectAW.Modules.Maestros.Ubicaciones.DTOs.Responses;
using ProjectAW.Modules.Maestros.Ubicaciones.Repositories;
using ProjectAW.Modules.Maestros.Ubicaciones.Entities;

namespace ProjectAW.Modules.Maestros.Ubicaciones.Services
{
    public class UbicacionService : IUbicacionService
    {
        private readonly IUbicacionRepository _repository;

        public UbicacionService(IUbicacionRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> ActualizarAsync(int id, ActualizarUbicacionDto dto)
        {
            var ubicacion = await _repository.ObtenerUbicacionPorIdAsync(id);
            
            if(ubicacion == null)
                return false;

            var codigo = dto.Codigo.Trim();

            var ubicacionConElMismoCodigo = await _repository.ObtenerUbicacionPorCodigoAsync(codigo);
            if(ubicacionConElMismoCodigo != null && ubicacionConElMismoCodigo.IdUbicacion != id)
            {
                throw new InvalidOperationException($"Ya existe una ubicación con el código '{codigo}'.");
            }

            ubicacion.Codigo = codigo;
            ubicacion.Descripcion = dto.Descripcion?.Trim();
            ubicacion.Activo = dto.Activo;
            ubicacion.FchModificacion = DateTime.UtcNow;

            await _repository.ActualizarUbicacionAsync(ubicacion);

            return true;

        }

        public async Task<UbicacionDto> CrearUbicacionAsync(CrearUbicacionDto dto)
        {
            var codigo = dto.Codigo.Trim();

            var ubicacionExistente = await _repository.ObtenerUbicacionPorCodigoAsync(codigo);
        
            if(ubicacionExistente != null)
                throw new InvalidOperationException($"Ya existe una ubicación con el código '{codigo}'.");

            var ubicacion = new Ubicacion
            {
                Codigo = codigo,
                Descripcion = dto.Descripcion?.Trim(),
                Activo = dto.Activo,
                FchRegistro = DateTime.Now
            };

            await _repository.CrearUbicacionAsync(ubicacion);

            return new UbicacionDto
            {
                IdUbicacion = ubicacion.IdUbicacion,
                Codigo = ubicacion.Codigo,
                Descripcion = ubicacion.Descripcion,
                Activo = ubicacion.Activo
            };
        }

        public async Task<bool> CambiarEstadoAsync(int id, bool activo)
        {
            var ubicacion = await _repository.ObtenerUbicacionPorIdAsync(id);
            
            if (ubicacion == null)
                return false;

            ubicacion.Activo = activo;
            //ubicacion.FchModificacion = DateTime.UtcNow; estaria buena evaluar aqui el cambio tambien de fecha 

            await _repository.ActualizarUbicacionAsync(ubicacion);
            return true;
        }

        public async Task<List<UbicacionDto>> ObtenerUbicacionesAsync()
        {
            var ubicaciones = await _repository.ListarUbicacionesAsync();
            return ubicaciones.Select(u => new UbicacionDto
            {
                IdUbicacion = u.IdUbicacion,
                Codigo = u.Codigo,
                Descripcion = u.Descripcion,
                Activo = u.Activo
            }).ToList();

        }

        public async Task<UbicacionDto?> ObtenerUbicacionPorCodigoAsync(string codigo)
        {
            var ubicacion = await _repository.ObtenerUbicacionPorCodigoAsync(codigo);

            if (ubicacion == null)
                return null;

            return new UbicacionDto
            {
                IdUbicacion = ubicacion.IdUbicacion,
                Codigo = ubicacion.Codigo,
                Descripcion = ubicacion.Descripcion,
                Activo = ubicacion.Activo
            };
        }

        public async Task<UbicacionDto?> ObtenerUbicacionPorIdAsync(int id)
        {
            var ubicacion = await _repository.ObtenerUbicacionPorIdAsync(id);

            if(ubicacion == null)
                return null;
            
            return new UbicacionDto
            {
                IdUbicacion = ubicacion.IdUbicacion,
                Codigo = ubicacion.Codigo,
                Descripcion = ubicacion.Descripcion,
                Activo = ubicacion.Activo
            };

        }
    }
}
