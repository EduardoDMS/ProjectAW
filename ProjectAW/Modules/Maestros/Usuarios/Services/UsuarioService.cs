using Microsoft.AspNetCore.Identity;
using ProjectAW.Modules.Maestros.Productos.Repositories;
using ProjectAW.Modules.Maestros.Usuarios.DTOs.Requests;
using ProjectAW.Modules.Maestros.Usuarios.DTOs.Responses;
using ProjectAW.Modules.Maestros.Usuarios.Entitites;
using ProjectAW.Modules.Maestros.Usuarios.Repositories;

namespace ProjectAW.Modules.Maestros.Usuarios.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _repository;
         
        
        public UsuarioService(IUsuarioRepository _repo)
        {
            _repository = _repo;
        }

        public async Task<bool> ActualizarUsuarioAsync(int id, ActualizarUsuarioDto dto)
        {
            var usuario = await _repository.GetByIdAsync(id);

            if (usuario == null)
                return false;

            var username = dto.Username.Trim();

            var usuarioConElmismoNombre = await _repository.ObtenerPorUsernameAsync(username);

            if(usuarioConElmismoNombre != null && usuarioConElmismoNombre.IdUsuario!= id)
            {
                throw new InvalidOperationException("Ya existe un usuario con ese username intenta con otro ");
            }

            usuario.Username = username;
            usuario.Nombre = dto.Nombre;
            usuario.Apellido = dto.Apellido;
            usuario.Password = dto.password;
            usuario.IdRol = dto.IdRol;
            usuario.FechaActualizada = DateTime.UtcNow;

            await _repository.ActualizarAsync(usuario);
            return true;
               
        }

        public async Task<bool> CambiarEstadoAsync(int id, bool activo)
        {
            var usuario = await _repository.GetByIdAsync(id);
            if (usuario == null)
                return false;

            usuario.Activo = activo;

            await _repository.ActualizarAsync(usuario);
            return true;
        }

        public async Task<UsuarioDto?> CrearUsuarioAsync(CrearUsuarioDto dto)
        {
            var username = dto.Username.Trim();
            var usuarioExistente = await _repository.ObtenerPorUsernameAsync(username);

            if(usuarioExistente != null)
            {
                throw new InvalidOperationException("Ya existe un usuario con este nombre");
            }

            var usuario = new Usuario
            {
                Username = username,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Password = dto.Password,
                IdRol = dto.IdRol,
                Activo = true,
                FechaRegistro = DateTime.Now
            };


            await _repository.CrearAsync(usuario);

            return new UsuarioDto
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Username = usuario.Username,
                IdRol = usuario.IdRol,
                Activo = usuario.Activo
            };

        }

        public async Task<List<UsuarioDto>> ListarUsuariosAsync()
        {
            var usuario = await _repository.GetAllAsync();

            return usuario.Select(u => new UsuarioDto
            {
                IdUsuario = u.IdUsuario,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Username = u.Username,
                Activo = u.Activo,
                IdRol = u.IdRol
            }).ToList();
        }

        public async Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int id)
        {
            var usuario = await _repository.GetByIdAsync(id);

            if (usuario == null)
                return null;

            return new UsuarioDto
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Username = usuario.Username,
                Activo = usuario.Activo,
                IdRol = usuario.IdRol

            };
        }

        public async Task<UsuarioDto?> ObtenerUsuarioUserAsync(string username)
        {
            var usuario = await _repository.ObtenerPorUsernameAsync(username);

            if (usuario == null)
                return null;

            return new UsuarioDto
            {
                IdUsuario = usuario.IdUsuario,
                Username = usuario.Username,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Activo = usuario.Activo,
                IdRol = usuario.IdRol

            };
        }
    }
}
