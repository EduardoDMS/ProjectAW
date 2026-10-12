using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Maestros.TipoDocumentos.DTOs.Requests;
using ProjectAW.Modules.Maestros.TipoDocumentos.Services;

namespace ProjectAW.Modules.Maestros.TipoDocumentos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TipoDocumentoApiController : ControllerBase
    {
        private readonly ITipoDocumentoService _service;

        public TipoDocumentoApiController(ITipoDocumentoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var tipodocumento = await _service.ObtenerTiposDocumentosAsync();
            return Ok(tipodocumento);
        }

        //[HttpPost]
        //public async Task<IActionResult> Crear([FromBody] CrearTipoDocumentoDto request)
        //{
        //    try
        //    {
        //        var tipoDocumento = await _service.CrearTipoDocumento(request);
        //        return Ok(tipoDocumento);
        //    }
        //    catch(InvalidOperationException ex)
        //    {
        //        return Conflict(new
        //        {
        //            mensaje = ex.Message
        //        });
        //    }
        //}



    }
}
