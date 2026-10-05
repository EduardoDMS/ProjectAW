using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Maestros.Almacenes.DTOs.Requests;
using ProjectAW.Modules.Maestros.Almacenes.Services;

namespace ProjectAW.Modules.Maestros.Almacenes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlmacenApiController : ControllerBase
    {
        private readonly IAlmacenService _service;
        
        public AlmacenApiController(IAlmacenService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var almacenes = await _service.ObtenerAlmacenesTodosAsync();
            return Ok(almacenes);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var almacen = await _service.ObtenerAlmacenPorIdAsync(id);

            if (almacen == null)
                return NotFound();
            
            return Ok(almacen);
        }


        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearAlmacenDto request)
        {
            try
            {
                var almacen = await _service.CrearAlmacenAsync(request);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = almacen.IdAlmacen },
                    almacen);
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
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarAlmacenDto request)
        {
            try
            {
                var actualizado = await _service.ActualizarAlmacenAsync(id, request);
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


        [HttpPatch("{id:int}")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool activo)
        {
            var cambiado = await _service.CambiarEstadoAlmacenAsync(id, activo);
            
            if (!cambiado)
                return NotFound();
            
            return NoContent();
        }



    }
}
