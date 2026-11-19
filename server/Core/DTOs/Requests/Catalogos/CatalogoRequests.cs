using RxBox.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RxBox.Core.DTOs.Requests.Catalogos
{
    // Tipo es nullable solo para que [Required] detecte cuando no viene.
    public record CatalogoCreateRequest(
        [Required] TipoCatalogo? Tipo,
        [Required, StringLength(60)] string Nombre,
        short? Orden);

    // Sin Tipo: no se puede cambiar después de crear (RN-001).
    public record CatalogoUpdateRequest(
        [Required, StringLength(60)] string Nombre,
        short? Orden);
}
