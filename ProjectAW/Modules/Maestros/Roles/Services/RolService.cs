using ProjectAW.Modules.Maestros.Roles.DTOs.Requests;
using ProjectAW.Modules.Maestros.Roles.DTOs.Response;
using ProjectAW.Modules.Maestros.Roles.Entities;
using ProjectAW.Modules.Maestros.Roles.Repositories;

namespace ProjectAW.Modules.Maestros.Roles.Services
{
    public class RolService : IRolService
    {
        private readonly IRolRepository _repo;

        public RolService(IRolRepository repo)
        {
            _repo = repo;
        }



        public async Task<bool> ActualizarRolAsync(int id, ActualizarRolDto dto)
        {
            var rol = await _repo.GetByIdAsync(id);

            if (rol == null)
                return false;

            var nombre = dto.Nombre.Trim();

            var rolConMismoNombre = await _repo.ObtenerPorNombreAsync(nombre);

            if(rolConMismoNombre != null && rolConMismoNombre.IdRol != id)
            {
                throw new InvalidOperationException("Ya existe un Rol con ese nombre ");
            }

            rol.Nombre = nombre;

            await _repo.ActualizarAsync(rol);

            return true;
        }

        public async Task<RolDto> CrearRolAsync(RolDto dto)
        {
            var nombre = dto.Nombre.Trim();

            var rolExistente = await _repo.ObtenerPorNombreAsync(nombre);

            if (rolExistente != null)
                throw new InvalidOperationException("Ya existe un producto con ese codigo");

            var rol = new Rol
            {
                Nombre = nombre,
            };

            await _repo.CrearAsync(rol);

            return new RolDto
            {
                IdRol = rol.IdRol,
                Nombre = rol.Nombre
            };
        }

        public Task<RolDto> CrearRolAsync(CrearRolDto dto)
        {
            throw new NotImplementedException();
        }

        public async Task<RolDto?> ObtenerRolPorIdAsync(int id)
        {
            var rol = await _repo.GetByIdAsync(id);

            if (rol == null)
                return null;

            return new RolDto
            {
                IdRol = rol.IdRol,
                Nombre = rol.Nombre
            };
        }

        public async Task<RolDto?> ObtenerRolPorNombreAsync(string nombre)
        {
            var rol = await _repo.ObtenerPorNombreAsync(nombre);

            if (rol == null)
                return null;
            return new RolDto
            {
                IdRol = rol.IdRol,
                Nombre = rol.Nombre
            };
        }

        public async Task<List<RolDto>> ObtenerTodosRolesAsync()
        {
            var roles = await _repo.GetAllAsync();

            return roles.Select(r => new RolDto
            {
                IdRol = r.IdRol,
                Nombre = r.Nombre
            }).ToList();
        }
    }
}
