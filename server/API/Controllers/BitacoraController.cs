using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.DTOs.Requests.Auditoria;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;

namespace RxBox.API.Controllers
{
    [ApiController]
    [Route("api/bitacora")]
    [Authorize(Roles = nameof(RolUsuario.ADMIN))]
    public class BitacoraController(IBitacoraService service) : ControllerBase
    {
        /// <summary>GET /api/bitacora?entidad=Persona&amp;entidadPublicId=…&amp;desde=2026-11-01&amp;hasta=2026-11-06&amp;pagina=1&amp;tamano=20</summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] BitacoraFiltroRequest filtro, CancellationToken ct) =>
            Ok(await service.ListarAsync(filtro, ct));
    }
}
