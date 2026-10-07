using ProjectAW.Modules.Maestros.Proveedores.DTOs.Requests;
using ProjectAW.Modules.Maestros.Proveedores.DTOs.Responses;
using ProjectAW.Modules.Maestros.Proveedores.Entities;
using ProjectAW.Modules.Maestros.Proveedores.Repositories;

namespace ProjectAW.Modules.Maestros.Proveedores.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly IProveedorRepository _repository;

        public ProveedorService(IProveedorRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> ActualizarAsync(int id, ActualizarProveedorDto dto)
        {
            var proveedor = await _repository.ObtenerPorIdAsync(id);

            if (proveedor == null)
                return false;

            var numeroDocumento = dto.NumeroDocumento.Trim();

            var clienteConMismoNumero = await _repository.ObtenerPorNumeroDocumentoAsync(numeroDocumento);

            if(clienteConMismoNumero != null && clienteConMismoNumero.IdProveedor!=id)
            {
                throw new InvalidOperationException("Ya existe un proveedor con ese número de documento.");
            }

            proveedor.RazonSocial = dto.RazonSocial;
            proveedor.IdTipoDocumento = dto.IdTipoDocumento;
            proveedor.NumeroDocumento = numeroDocumento;
            proveedor.Direccion = dto.Direccion;
            proveedor.NombreContacto = dto.NombreContacto;
            proveedor.CorreoContacto = dto.CorreoContacto;
            proveedor.TelefonoContacto = dto.TelefonoContacto;
            proveedor.Activo = dto.Activo;

            proveedor.FchModificacion = DateTime.Now;

            await _repository.ActualizarAsync(proveedor);

            return true;
        }

        public async Task<bool> CambiarEstadoAsync(int id, bool activo)
        {
            var proveedor = await _repository.ObtenerPorIdAsync(id);

            if (proveedor == null)
                return false;

            proveedor.Activo = activo;

            await _repository.ActualizarAsync(proveedor);

            return true;
        }

        public async Task<ProveedorDto> CrearAsync(CrearProveedorDto dto)
        {
            var numeroDocumento = dto.NumeroDocumento.Trim();

            var proveedorExistente=await _repository.ObtenerPorNumeroDocumentoAsync(numeroDocumento);

            if (proveedorExistente != null)
                throw new InvalidOperationException("Ya existe un proveedor con ese número de documento.");

            var proveedor = new Proveedor
            {
                RazonSocial=dto.RazonSocial,
                IdTipoDocumento=dto.IdTipoDocumento,
                NumeroDocumento=numeroDocumento,
                Direccion=dto.Direccion,
                NombreContacto=dto.NombreContacto,
                CorreoContacto=dto.CorreoContacto,
                TelefonoContacto=dto.TelefonoContacto,
                Activo=true,
                FchRegistro=DateTime.UtcNow
            };

            await _repository.CrearAsync(proveedor);

            return new ProveedorDto
            {
                IdProveedor = proveedor.IdProveedor,
                RazonSocial = proveedor.RazonSocial,
                IdTipoDocumento = proveedor.IdTipoDocumento,
                NumeroDocumento = proveedor.NumeroDocumento,
                Direccion = proveedor.Direccion,
                NombreContacto = proveedor.NombreContacto,
                CorreoContacto = proveedor.CorreoContacto,
                TelefonoContacto = proveedor.TelefonoContacto,
                Activo = proveedor.Activo
            };
        }

        public async Task<ProveedorDto?> ObtenerPorIdAsync(int id)
        {
            var proveedor = await _repository.ObtenerPorIdAsync(id);

            if(proveedor==null)
                return null;

            return new ProveedorDto
            {
                IdProveedor = proveedor.IdProveedor,
                RazonSocial = proveedor.RazonSocial,
                IdTipoDocumento = proveedor.IdTipoDocumento,
                NumeroDocumento = proveedor.NumeroDocumento,
                Direccion = proveedor.Direccion,
                NombreContacto=proveedor.NombreContacto,
                CorreoContacto = proveedor.CorreoContacto,
                TelefonoContacto = proveedor.TelefonoContacto,           
                Activo = proveedor.Activo
            };
        }

        public async Task<ProveedorDto?> ObtenerPorNumeroDocumento(string numeroDocumento)
        {
            var proveedor = await _repository.ObtenerPorNumeroDocumentoAsync(numeroDocumento);

            if (proveedor == null)
                return null;

            return new ProveedorDto
            {
                IdProveedor = proveedor.IdProveedor,
                RazonSocial=proveedor.RazonSocial,
                IdTipoDocumento=proveedor.IdTipoDocumento,
                NumeroDocumento=proveedor.NumeroDocumento,
                Direccion = proveedor.Direccion,
                NombreContacto = proveedor.NombreContacto,
                CorreoContacto =proveedor.CorreoContacto,
                TelefonoContacto=proveedor.TelefonoContacto,      
                Activo=proveedor.Activo
            };
        }

        public async Task<List<ProveedorDto>> ObtenerTodosAsync()
        {
            var proveedores = await _repository.ObtenerTodosAsync();

            return proveedores.Select(p => new ProveedorDto
            {
                IdProveedor=p.IdProveedor,
                RazonSocial=p.RazonSocial,
                IdTipoDocumento=p.IdTipoDocumento,
                NumeroDocumento=p.NumeroDocumento,
                Direccion=p.Direccion,
                NombreContacto=p.NombreContacto,
                CorreoContacto=p.CorreoContacto,
                TelefonoContacto=p.TelefonoContacto,
                Activo=p.Activo
            }).ToList();
        }
    }
}
