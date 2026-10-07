using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Maestros.Proveedores.DTOs.Requests;
using ProjectAW.Modules.Maestros.Proveedores.Services;

namespace ProjectAW.Modules.Maestros.Proveedores.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedorApiController : ControllerBase
    {
        private readonly IProveedorService _service;

        public ProveedorApiController(IProveedorService service)
        {
            _service=service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var proveedores=await _service.ObtenerTodosAsync();

            return Ok(proveedores);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var proveedor = await _service.ObtenerPorIdAsync(id);

            if(proveedor==null)
                return NotFound();

            return Ok(proveedor);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearProveedorDto request)
        {
            try
            {
                var proveedor=await _service.CrearAsync(request);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = proveedor.IdProveedor },
                    proveedor);
            }
            catch(InvalidOperationException ex)
            {
                return Conflict(new
                {
                    mensaje = ex.Message
                });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarProveedorDto request)
        {
            try
            {
                var actualizado=await _service.ActualizarAsync(id,request);

                if (!actualizado)
                    return NotFound();

                return NoContent();
            }
            catch(InvalidOperationException ex)
            {
                return Conflict(new
                {
                    mensaje = ex.Message
                });
            }
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool activo)
        {
            var actualizado = await _service.CambiarEstadoAsync(id,activo);

            if (!actualizado)
                return NotFound();

            return NoContent();
        }
    }
}
