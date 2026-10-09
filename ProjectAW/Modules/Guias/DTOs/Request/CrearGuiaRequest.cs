using ProjectAW.Modules.Guias.Entities;

namespace ProjectAW.Modules.Guias.DTOs.Request
{
    public class CrearGuiaRequest : ActualizarGuiaRequest
    {
       public int IdTipoOperacionGuia { get; set; }
    }
}
