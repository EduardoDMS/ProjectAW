using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Guias.DTOs.Request;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Guias.Services;
using ProjectAW.Models;

namespace ProjectAW.Modules.Guias.Controllers
{

    [ApiController]
    [Route("api/guias")]
    public class GuiasApiController : ControllerBase
    {
        private readonly IGuiaService _service;

        public GuiasApiController(IGuiaService service) => _service = service;

        [HttpPost]
        public Task<IActionResult> Crear(CrearGuiaRequest request, CancellationToken ct) =>
            Ejecutar(async () =>
            {
                var guia = await _service.CrearAsync(request, ct);
                return CreatedAtAction(nameof(ObtenerPorId), new { id = guia.IdGuia }, guia);
            });



        [HttpGet]
        public Task<IActionResult> Listar([FromQuery] int? idTipoOperacionGuia, [FromQuery] int? idEstadoGuia, CancellationToken ct) =>
            Ejecutar(async () => Ok(await _service.ListarAsync(idTipoOperacionGuia, idEstadoGuia, ct)));


        [HttpGet("{id:int}")]
        public Task<IActionResult> ObtenerPorId(int id, CancellationToken ct) =>
            Ejecutar(async () => Ok(await _service.ObtenerPorIdAsync(id, ct)));



        [HttpPut("{id:int}")]
        public Task<IActionResult> Actualizar(int id, ActualizarGuiaRequest request, CancellationToken ct) =>
            Ejecutar(async () => Ok(await _service.ActualizarAsync(id, request, ct)));



        [HttpPut("{id:int}/cancelar")]
        public Task<IActionResult> Cancelar(int id, CancellationToken ct) =>
            Ejecutar(async () =>
            {
                await _service.CancelarAsync(id, ct);
                return NoContent();
            });


        private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
        {
            try
            {
                return await accion();
            }
            catch (NoEncontradoException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "No encontrado");
            }
            catch (ReglaNegocioException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Regla de negocio");
            }
        }
    }
}
