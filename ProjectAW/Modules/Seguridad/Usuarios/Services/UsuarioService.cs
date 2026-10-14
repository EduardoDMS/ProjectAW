using Microsoft.AspNetCore.Identity;
using ProjectAW.Modules.Maestros.Productos.Repositories;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;
using ProjectAW.Modules.Seguridad.Usuarios.DTOs.Requests;
using ProjectAW.Modules.Seguridad.Usuarios.DTOs.Responses;
using ProjectAW.Modules.Seguridad.Usuarios.Repositories;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Models;

namespace ProjectAW.Modules.Seguridad.Usuarios.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _repository;
        private readonly IPasswordHasher<Usuario> _hasher;
         
        
        public UsuarioService(IUsuarioRepository _repo,IPasswordHasher<Usuario> hasher)
        {
            _repository = _repo;
            _hasher = hasher;
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
                throw new ConflictoNegocioException("Ya existe un usuario con ese nombre de usuario.");
            }

            usuario.Username = username;
            usuario.Nombre = dto.Nombre;
            usuario.Apellido = dto.Apellido;
           // usuario.Password = dto.password;
            usuario.IdRol = dto.IdRol;
            usuario.FechaActualizada = dto.FechaActualizacion;

            if (!string.IsNullOrWhiteSpace(dto.Password))
                usuario.Password = _hasher.HashPassword(usuario, dto.Password);

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
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ReglaNegocioException("La contraseña es obligatoria.");

            var username = dto.Username.Trim();
            var usuarioExistente = await _repository.ObtenerPorUsernameAsync(username);

            if(usuarioExistente != null)
            {
                throw new ConflictoNegocioException("Ya existe un usuario con ese nombre de usuario.");
            }

            var usuario = new Usuario
            {
                Username = username,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
               // Password = dto.Password,
                IdRol = dto.IdRol,
                Activo = true,
                FechaRegistro = DateTime.Now
            };

            usuario.Password = _hasher.HashPassword(usuario, dto.Password);

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
