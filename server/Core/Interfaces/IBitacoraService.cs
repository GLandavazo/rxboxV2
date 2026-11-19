using RxBox.Core.DTOs.Requests.Auditoria;
using RxBox.Core.DTOs.Responses.Auditoria;
using RxBox.Core.DTOs.Responses.Comun;
using System;
using System.Collections.Generic;
using System.Text;

namespace RxBox.Core.Interfaces
{
    public interface IBitacoraService
    {
        Task<PaginaResponse<BitacoraResponse>> ListarAsync(BitacoraFiltroRequest filtro, CancellationToken ct = default);
    }
}
