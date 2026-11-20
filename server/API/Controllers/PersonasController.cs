using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.Interfaces;

namespace RxBox.API.Controllers
{
    [ApiController]
    [Route("api/personas")]
    [Authorize]
    public class PersonasController(IPersonaService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] PersonaFiltroRequest filtro, CancellationToken ct) =>
            Ok(await service.ListarAsync(filtro, ct));

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Obtener(Guid publicId, CancellationToken ct) =>
            Ok(await service.ObtenerAsync(publicId, ct));
    }
}
