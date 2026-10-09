using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Maestros.Ubicaciones.DTOs.Requests;
using ProjectAW.Modules.Maestros.Ubicaciones.Services;

namespace ProjectAW.Modules.Maestros.Ubicaciones.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class UbicacionApiController : ControllerBase
    {
        private readonly IUbicacionService _service;

        public UbicacionApiController(IUbicacionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var ubicaciones = await _service.ObtenerUbicacionesAsync();
            return Ok(ubicaciones);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var ubicacion = await _service.ObtenerUbicacionPorIdAsync(id);
            if (ubicacion == null)
            {
                return NotFound();
            }
            return Ok(ubicacion);
        }


        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearUbicacionDto request)
        {
            try
            {
                var ubicacion = await _service.CrearUbicacionAsync(request);
                
                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = ubicacion.IdUbicacion },
                    ubicacion);
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
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarUbicacionDto request)
        {
            try
            {
                var resultado = await _service.ActualizarAsync(id, request);

                if (!resultado)
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
        public async Task<IActionResult> CambiarEstado(int id, [FromQuery] bool activo)
        {
            var resultado = await _service.CambiarEstadoAsync(id, activo);
            if (!resultado)
                return NotFound();
            return NoContent();
        }

    }
}
