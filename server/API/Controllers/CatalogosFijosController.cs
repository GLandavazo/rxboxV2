using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.Interfaces;

namespace RxBox.API.Controllers
{
    [ApiController]
    [Route("api/catalogos-fijos")]
    [AllowAnonymous]
    public class CatalogosFijosController(ICatalogoFijoService service) : ControllerBase
    {
        [HttpGet]
        public IActionResult Nombres() => Ok(service.Nombres());

        [HttpGet("{nombre}")]
        public IActionResult Obtener(string nombre) => Ok(service.Obtener(nombre));
    }
}
