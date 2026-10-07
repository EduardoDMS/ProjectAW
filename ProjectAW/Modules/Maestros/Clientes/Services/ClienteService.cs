using ProjectAW.Modules.Maestros.Clientes.DTOs.Requests;
using ProjectAW.Modules.Maestros.Clientes.DTOs.Responses;
using ProjectAW.Modules.Maestros.Clientes.Entities;
using ProjectAW.Modules.Maestros.Clientes.Repositories;

namespace ProjectAW.Modules.Maestros.Clientes.Services
{
    public class ClienteService:IClienteService
    {
        private readonly IClienteRepository _repository;

        public ClienteService(IClienteRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> ActualizarAsync(int id, ActualizarClienteDto dto)
        {
            var cliente = await _repository.ObtenerPorIdAsync(id);

            if (cliente == null)
                return false;

            var numeroDocumento = dto.NumeroDocumento.Trim();

            var clienteConMismoNumero = await _repository.ObtenerPorIdAsync(id);

            if(clienteConMismoNumero!=null && clienteConMismoNumero.IdCliente!=id)
            {
                throw new InvalidOperationException("Ya existe un cliente con ese número de documento.");
            }

            cliente.RazonSocial=dto.RazonSocial;
            cliente.IdTipoDocumento = dto.IdTipoDocumento;
            cliente.NumeroDocumento = dto.NumeroDocumento;
            cliente.Correo=dto.Correo;
            cliente.Telefono=dto.Telefono;
            cliente.Direccion=dto.Direccion;
            cliente.Activo=dto.Activo;
            cliente.FchModificacion=dto.FchModificacion;

            await _repository.ActualizarAsync(cliente);

            return true;
        }

        public async Task<bool> CambiarEstadoAsync(int id, bool activo)
        {
            var cliente = await _repository.ObtenerPorIdAsync(id);

            if (cliente == null)
                return false;

            cliente.Activo = activo;

            await _repository.ActualizarAsync(cliente);

            return true;
        }

        public async Task<ClienteDto> CrearAsync(CrearClienteDto dto)
        {
            var numeroDocumento= dto.NumeroDocumento.Trim();

            var clienteExistente = await _repository.ObtenerPorNumeroDocumentoAsync(numeroDocumento);

            if (clienteExistente != null)
                throw new InvalidOperationException("Ya existe un cliente con ese número de documento.");

            var cliente = new Cliente
            {
                RazonSocial=dto.RazonSocial,
                IdTipoDocumento=dto.IdTipoDocumento,
                NumeroDocumento=numeroDocumento,
                Correo=dto.Correo,
                Telefono=dto.Telefono,
                Direccion=dto.Direccion,
                Activo=true,
                FchRegistro=DateTime.UtcNow
            };

            await _repository.CrearAsync(cliente);

            return new ClienteDto
            {
                IdCliente=cliente.IdCliente,
                RazonSocial=cliente.RazonSocial,
                IdTipoDocumento=cliente.IdTipoDocumento,
                NumeroDocumento=cliente.NumeroDocumento,
                Correo=cliente.Correo,
                Telefono=cliente.Telefono,
                Direccion=cliente.Direccion,
                Activo=cliente.Activo
            };
        }

        public async Task<ClienteDto?> ObtenerPorIdAsync(int id)
        {
            var cliente = await _repository.ObtenerPorIdAsync(id);

            if (cliente == null)
                return null;

            return new ClienteDto
            {
                IdCliente = cliente.IdCliente,
                RazonSocial = cliente.RazonSocial,
                IdTipoDocumento = cliente.IdTipoDocumento,
                NumeroDocumento = cliente.NumeroDocumento,
                Correo = cliente.Correo,
                Telefono = cliente.Telefono,
                Direccion = cliente.Direccion,
                Activo = cliente.Activo
            };
        }

        public async Task<ClienteDto?> ObtenerPorNumeroDocumento(string numeroDocumento)
        {
            var cliente = await _repository.ObtenerPorNumeroDocumentoAsync(numeroDocumento);

            if (cliente == null)
                return null;

            return new ClienteDto
            {
                IdCliente = cliente.IdCliente,
                RazonSocial = cliente.RazonSocial,
                IdTipoDocumento = cliente.IdTipoDocumento,
                NumeroDocumento = cliente.NumeroDocumento,
                Correo = cliente.Correo,
                Telefono = cliente.Telefono,
                Direccion = cliente.Direccion,
                Activo = cliente.Activo
            };
        }

        public async Task<List<ClienteDto>> ObtenerTodosAsync()
        {
            var clientes = await _repository.ObtenerTodosAsync();

            return clientes.Select(c => new ClienteDto
            {
                IdCliente = c.IdCliente,
                RazonSocial = c.RazonSocial,
                IdTipoDocumento = c.IdTipoDocumento,
                NumeroDocumento = c.NumeroDocumento,
                Correo = c.Correo,
                Telefono = c.Telefono,
                Direccion = c.Direccion,
                Activo = c.Activo
            }).ToList();
        }
    }
}
