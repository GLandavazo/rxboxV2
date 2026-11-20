using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.Enums;
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

        [HttpPost]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> Crear([FromBody] PersonaCreateRequest request, CancellationToken ct)
        {
            var creada = await service.CrearAsync(request, ct);
            return CreatedAtAction(nameof(Obtener), new { publicId = creada.PublicId }, creada);
        }

        [HttpPut("{publicId:guid}")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> Editar(Guid publicId, [FromBody] PersonaUpdateRequest request, CancellationToken ct) =>
            Ok(await service.EditarAsync(publicId, request, ct));

        [HttpPatch("{publicId:guid}/toggle")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> CambiarEstado(Guid publicId, CancellationToken ct) =>
            Ok(await service.CambiarEstadoAsync(publicId, ct));





    }
}
