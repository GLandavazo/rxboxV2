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

        #region Contacto de Emergencia

        [HttpPost("{publicId:guid}/contactos")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> AgregarContacto(
        Guid publicId, [FromBody] ContactoEmergenciaRequest request, CancellationToken ct)
            {
                var contacto = await service.AgregarContactoAsync(publicId, request, ct);
                return CreatedAtAction(nameof(Obtener), new { publicId }, contacto);
            }

        [HttpPut("{publicId:guid}/contactos/{contactoPublicId:guid}")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> EditarContacto(
            Guid publicId, Guid contactoPublicId, [FromBody] ContactoEmergenciaRequest request, CancellationToken ct) =>
            Ok(await service.EditarContactoAsync(publicId, contactoPublicId, request, ct));

        [HttpDelete("{publicId:guid}/contactos/{contactoPublicId:guid}")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> EliminarContacto(Guid publicId, Guid contactoPublicId, CancellationToken ct)
        {
            await service.EliminarContactoAsync(publicId, contactoPublicId, ct);
            return NoContent();
        }

        #endregion




    }
}
