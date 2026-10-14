using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Maestros.Categorias.DTOs.Requests;
using ProjectAW.Modules.Maestros.Categorias.Services;

namespace ProjectAW.Modules.Maestros.Categorias.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriaApiController : ControllerBase
    {
        private readonly ICategoriaService _service;

        public CategoriaApiController(ICategoriaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodasCategorias()
        {
            var categorias = await _service.ListarCategoriasAsync();
            return Ok(categorias);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerCategiriaId(int id)
        {
            var categoria = await _service.ObtenerCategoriaPorIdAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }
            return Ok(categoria);
        }

        [HttpPost]
        public async Task<IActionResult> CrearCategoria([FromBody] CrearCategoriaDto dto)
        {
            var categoriaCreada = await _service.CrearCategoriaAsync(dto);
            return CreatedAtAction(nameof(ObtenerCategiriaId), new { id = categoriaCreada.IdCategoria }, categoriaCreada);
        }


        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarCategoria(int id ,[FromBody] ActualizarCategoriaDto request)
        {
            var actualizado = await _service.ActualizarCategoriaAsync(id, request);
            if (!actualizado)
                return NotFound();

            return NoContent();
        }




        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstadoCategoria(int id, [FromBody] bool activo)
        {
            var actualizado = await _service.CambiarEstadoCategoriaAsync(id, activo);

            if (!actualizado)
                return NotFound();

            return NoContent();
        }

    }
}
