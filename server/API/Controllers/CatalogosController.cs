using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.DTOs.Requests.Catalogos;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;

namespace RxBox.API.Controllers
{
    [ApiController]
    [Route("api/catalogos")]
    [Authorize]
    public class CatalogosController(ICatalogoService service) : ControllerBase
    {
        /// <summary>GET /api/catalogos?tipo=METODO_PAGO&amp;soloActivos=true</summary>
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] TipoCatalogo? tipo, [FromQuery] bool soloActivos = true, CancellationToken ct = default) =>
            Ok(await service.ListarAsync(tipo, soloActivos, ct));

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Obtener(Guid publicId, CancellationToken ct) =>
            Ok(await service.ObtenerAsync(publicId, ct));

        [HttpPost]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> Crear([FromBody] CatalogoCreateRequest request, CancellationToken ct)
        {
            var creado = await service.CrearAsync(request, ct);
            return CreatedAtAction(nameof(Obtener), new { publicId = creado.PublicId }, creado);
        }

        [HttpPut("{publicId:guid}")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> Editar(Guid publicId, [FromBody] CatalogoUpdateRequest request, CancellationToken ct) =>
            Ok(await service.EditarAsync(publicId, request, ct));

        [HttpPatch("{publicId:guid}/toggle")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> CambiarEstado(Guid publicId, CancellationToken ct) =>
            Ok(await service.CambiarEstadoAsync(publicId, ct));


    }
}
