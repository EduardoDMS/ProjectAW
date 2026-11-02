using ProjectAW.Modules.Maestros.Almacenes.DTOs.Requests;
using ProjectAW.Modules.Maestros.Almacenes.DTOs.Responses;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Almacenes.Repositories;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Infrastructure.Data;

namespace ProjectAW.Modules.Maestros.Almacenes.Services
{
    public class AlmacenService : IAlmacenService
    { 
        private readonly IAlmacenRepository _repository;
        private readonly ITransactionManager _transactions;

        public AlmacenService(IAlmacenRepository repository, ITransactionManager transactions)
        {
            _repository = repository;
            _transactions = transactions;
        }

        public async Task<bool> ActualizarAlmacenAsync(int id, ActualizarAlmacenDto dto)
        {
            var actualizado = false;

            await _transactions.ExecuteAsync(async _ =>
            {
                var almacen = await _repository.ObtenerAlmacenParaActualizarAsync(id);
                if (almacen is null)
                    return;

                var codigo = dto.Codigo.Trim();
                await ValidarCodigoUnicoAsync(id, codigo);

                var ubicacionesSeleccionadas = (dto.IdUbicacionesDespacho ?? []).ToHashSet();
                await ValidarUbicacionesDespachoAsync(id, ubicacionesSeleccionadas);

                almacen.Codigo = codigo;
                almacen.Descripcion = dto.Descripcion?.Trim();
                almacen.Activo = dto.Activo;
                almacen.FchModificacion = DateTime.UtcNow;
                await _repository.ActualizarAlmacenAsync(almacen);
                await _repository.EliminarUbicacionesDespachoAsync(id);
                await _repository.AgregarUbicacionesDespachoAsync(id, ubicacionesSeleccionadas);
                actualizado = true;
            });

            return actualizado;
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

        private async Task ValidarCodigoUnicoAsync(int idAlmacen, string codigo)
        {
            var almacenExistente = await _repository.ObtenerAlmacenPorCodigoAsync(codigo);
            if (almacenExistente is not null && almacenExistente.IdAlmacen != idAlmacen)
                throw new ConflictoNegocioException("El código del almacén ya existe.");
        }

        private async Task ValidarUbicacionesDespachoAsync(
            int idAlmacen, IReadOnlySet<int> idsSeleccionados)
        {
            var idsValidos = await _repository.ListarUbicacionesDelAlmacenAsync(
                idAlmacen, idsSeleccionados);
            if (idsSeleccionados.Except(idsValidos).Any())
                throw new ReglaNegocioException("Selecciona únicamente ubicaciones pertenecientes a este almacén.");
        }

    }
}
