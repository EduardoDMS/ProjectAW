using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Maestros.Productos.DTOs.Requests;
using ProjectAW.Modules.Maestros.Productos.Services;

namespace ProjectAW.Modules.Maestros.Productos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoApiController : ControllerBase
    {
        private readonly IProductoService _service;

        public ProductoApiController(IProductoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var productos = await _service.ObtenerTodosAsync();

            return Ok(productos);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var producto = await _service.ObtenerPorIdAsync(id);

            if (producto == null)
                return NotFound();

            return Ok(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearProductoDto request)
        {
            try
            {
                var producto = await _service.CrearAsync(request);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = producto.IdProducto },
                    producto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    mensaje = ex.Message
                });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarProductoDto request)
        {
            try
            {
                var actualizado=await _service.ActualizarAsync(id,request);

                if(!actualizado)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    mensaje = ex.Message
                });
            }
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool activo) {
            var actualizado=await _service.CambiarEstadoAsync(id,activo);

            if (!actualizado)
                return NotFound();

            return NoContent();
        }
    }
}
