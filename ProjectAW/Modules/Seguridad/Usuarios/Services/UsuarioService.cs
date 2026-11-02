using Microsoft.AspNetCore.Identity;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Maestros.Roles.Services;
using ProjectAW.Modules.Seguridad.Usuarios.DTOs.Requests;
using ProjectAW.Modules.Seguridad.Usuarios.DTOs.Responses;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;
using ProjectAW.Modules.Seguridad.Usuarios.Repositories;

namespace ProjectAW.Modules.Seguridad.Usuarios.Services;

public class UsuarioService : IUsuarioService
{
    private static readonly string[] RolesAdministrables =
    [
        "Operador",
        "Administrador",
        "Administrador inicial",
        "Admin"
    ];

    private readonly IUsuarioRepository _repository;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly IRolService _rolService;

    public UsuarioService(
        IUsuarioRepository repository,
        IPasswordHasher<Usuario> hasher,
        IRolService rolService)
    {
        _repository = repository;
        _hasher = hasher;
        _rolService = rolService;
    }

    public async Task<bool> ActualizarUsuarioAsync(int id, ActualizarUsuarioDto dto)
    {
        ValidarDatos(dto.Nombre, dto.Apellido, dto.Username);
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return false;

        // Confirma que el rol sea asignable y que el nombre de usuario no pertenezca a otra cuenta.
        await ValidarRolAsync(dto.IdRol);
        var username = dto.Username.Trim();
        var usuarioConElmismoNombre = await _repository.ObtenerPorUsernameAsync(username);
        if (usuarioConElmismoNombre is not null && usuarioConElmismoNombre.IdUsuario != id)
            throw new ConflictoNegocioException("Ya existe un usuario con ese nombre de usuario.");

        usuario.Username = username;
        usuario.Nombre = dto.Nombre.Trim();
        usuario.Apellido = dto.Apellido.Trim();
        usuario.IdRol = dto.IdRol;
        usuario.FechaActualizada = DateTime.UtcNow;

        // Una contraseña vacía significa conservar la actual; si llega una nueva, se guarda hasheada.
        if (!string.IsNullOrWhiteSpace(dto.Password))
            usuario.Password = _hasher.HashPassword(usuario, dto.Password);

        await _repository.ActualizarAsync(usuario);
        return true;
    }

    public async Task<bool> CambiarEstadoAsync(int id, bool activo)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return false;
        usuario.Activo = activo;
        await _repository.ActualizarAsync(usuario);
        return true;
    }

    public async Task<UsuarioDto?> CrearUsuarioAsync(CrearUsuarioDto dto)
    {
        ValidarDatos(dto.Nombre, dto.Apellido, dto.Username);
        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ReglaNegocioException("La contraseña es obligatoria.");

        await ValidarRolAsync(dto.IdRol);
        var username = dto.Username.Trim();
        if (await _repository.ObtenerPorUsernameAsync(username) is not null)
            throw new ConflictoNegocioException("Ya existe un usuario con ese nombre de usuario.");

        var usuario = new Usuario
        {
            Username = username,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            IdRol = dto.IdRol,
            Activo = true,
            FechaRegistro = DateTime.Now
        };
        usuario.Password = _hasher.HashPassword(usuario, dto.Password);
        await _repository.CrearAsync(usuario);
        return await ObtenerUsuarioPorIdAsync(usuario.IdUsuario);
    }

    public async Task<List<UsuarioDto>> ListarUsuariosAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto).ToList();

    public async Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id);
        return usuario is null ? null : ToDto(usuario);
    }

    public async Task<UsuarioDto?> ObtenerUsuarioUserAsync(string username)
    {
        var usuario = await _repository.ObtenerPorUsernameAsync(username);
        return usuario is null ? null : ToDto(usuario);
    }

    private async Task ValidarRolAsync(int idRol)
    {
        var rol = await _rolService.ObtenerRolPorIdAsync(idRol);
        if (rol is null)
            throw new ReglaNegocioException("Selecciona un rol válido.");

        var nombre = rol.Nombre.Trim();
        var permitido = RolesAdministrables.Any(rolPermitido =>
            nombre.Equals(rolPermitido, StringComparison.OrdinalIgnoreCase));
        if (!permitido)
            throw new ReglaNegocioException("Solo se puede asignar el rol Administrador o Operador.");
    }

    private static void ValidarDatos(string nombre, string apellido, string username)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(username))
            throw new ReglaNegocioException("Nombre, apellido y nombre de usuario son obligatorios.");
        if (nombre.Trim().Length > 100 || apellido.Trim().Length > 100 || username.Trim().Length > 20)
            throw new ReglaNegocioException("El nombre y apellido admiten hasta 100 caracteres; el usuario, hasta 20.");
    }

    private static UsuarioDto ToDto(Usuario usuario) => new()
    {
        IdUsuario = usuario.IdUsuario,
        Nombre = usuario.Nombre,
        Apellido = usuario.Apellido,
        Username = usuario.Username,
        IdRol = usuario.IdRol,
        NombreRol = usuario.rol?.Nombre ?? string.Empty,
        Activo = usuario.Activo
    };
}
