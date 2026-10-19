using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using ProjectAW.Modules.Maestros.Clientes.DTOs.Requests;
using ProjectAW.Modules.Maestros.Clientes.Services;

namespace ProjectAW.Modules.Maestros.Clientes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PwaRead")]
    public class ClienteApiController : ControllerBase
    {
        private readonly IClienteService _service;

        public ClienteApiController(IClienteService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var clientes = await _service.ObtenerTodosAsync();

            return Ok(clientes);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var cliente = await _service.ObtenerPorIdAsync(id);

            if (cliente == null)
                return NotFound();

            return Ok(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearClienteDto request)
        {
            var cliente = await _service.CrearAsync(request);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = cliente.IdCliente }, cliente);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarClienteDto request)
        {
            var actualizado = await _service.ActualizarAsync(id, request);
            if (!actualizado)
                return NotFound();

            return NoContent();
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool activo)
        {
            var actualizado = await _service.CambiarEstadoAsync(id,activo);

            if(!actualizado)
                return NotFound();

            return NoContent();
        }
    }
}
