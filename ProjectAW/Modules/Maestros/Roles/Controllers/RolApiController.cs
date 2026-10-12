using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Maestros.Roles.DTOs.Requests;
using ProjectAW.Modules.Maestros.Roles.Services;

namespace ProjectAW.Modules.Maestros.Roles.Controllers
{
    [ApiController]
    [Route("api/controller")]
    public class RolApiController : ControllerBase
    {
        private readonly IRolService _service;

        public RolApiController(IRolService service)
        {
            _service = service;
        }


        [HttpGet]
        public async Task<IActionResult> ObtenerTodas()
        {
            var roles = await _service.ObtenerTodosRolesAsync();
            return Ok(roles);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var rol = await _service.ObtenerRolPorIdAsync(id);

            if (rol == null)
                return NotFound();

            return Ok(rol);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearRolDto request)
        {
            try
            {
                var rol = await _service.CrearRolAsync(request);
                return CreatedAtAction(
                        nameof(ObtenerPorId),
                        new { id = rol.IdRol },
                        rol);
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
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarRolDto request)
        {
            try
            {
                var actualizado = await _service.ActualizarRolAsync(id, request);

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




    }
}
