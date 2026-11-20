using Microsoft.EntityFrameworkCore;
using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.DTOs.Responses.Comun;
using RxBox.Core.DTOs.Responses.Personas.RxBox.Core.DTOs.Responses.Personas;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Core.Interfaces.Repositories;
using RxBox.Infrastructure.Data;
using RxBox.Infrastructure.Services.Comun;
using System;
using System.Collections.Generic;
using System.Text;

namespace RxBox.Infrastructure.Services.Personas
{
    /// <summary>
    /// Personas por medio del UnitOfWork. AppDbContext se usa solo para usuario_boxes y
    /// refresh_tokens, que no son BaseEntity; es la misma instancia que usa el UnitOfWork.
    /// </summary>
    public class PersonaService(IUnitOfWork uow, AppDbContext db, ICurrentUserService currentUser) : IPersonaService
    {
        private const int MaxContactos = 3;

        private IGenericRepository<Persona> Personas => uow.Repository<Persona>();
        private IGenericRepository<ContactoEmergencia> Contactos => uow.Repository<ContactoEmergencia>();

        public async Task<PaginaResponse<PersonaListItemResponse>> ListarAsync(
            PersonaFiltroRequest f, CancellationToken ct = default)
        {
            var query = Personas.AsQueryable().AsNoTracking();

            if (f.SoloActivos) query = query.Where(p => p.Activo);

            if (f.Tipo == TipoPersona.ATLETA) query = query.Where(p => p.EsAtleta);
            else if (f.Tipo == TipoPersona.COACH) query = query.Where(p => p.EsCoach);

            if (!string.IsNullOrWhiteSpace(f.Buscar))
            {
                var patron = $"%{Texto.Normalizar(f.Buscar)}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.Nombre + " " + p.Apellidos, patron)
                    || EF.Functions.ILike(p.Codigo, patron)
                    || (p.Email != null && EF.Functions.ILike(p.Email, patron))
                    || (p.Telefono != null && EF.Functions.ILike(p.Telefono, patron)));
            }

            return await query
                .OrderBy(p => p.Apellidos).ThenBy(p => p.Nombre).ThenBy(p => p.Id)   // Id: orden estable entre páginas
                .Select(p => new PersonaListItemResponse(
                    p.PublicId, p.Codigo, p.Nombre, p.Apellidos, p.Email, p.Telefono, p.EsAtleta, p.EsCoach, p.Activo))
                .ToPaginaAsync(f, ct);
        }

        public async Task<PersonaResponse> ObtenerAsync(Guid publicId, CancellationToken ct = default)
        {
            var persona = await ObtenerEntidadAsync(publicId, ct);

            var contactos = await Contactos.AsQueryable().AsNoTracking()
                .Where(c => c.PersonaId == persona.Id)
                .OrderBy(c => c.Id)
                .Select(c => new ContactoEmergenciaResponse(c.PublicId, c.Nombre, c.Parentesco, c.Telefono))
                .ToListAsync(ct);

            return new PersonaResponse(
                persona.PublicId, persona.Codigo, persona.Nombre, persona.Apellidos,
                persona.Email, persona.Telefono, persona.EsAtleta, persona.EsCoach, persona.Activo,
                persona.FechaCreacion, contactos);
        }

        // Query filter de tenant: un publicId de otro box también da 404.
        private async Task<Persona> ObtenerEntidadAsync(Guid publicId, CancellationToken ct) =>
            await Personas.GetByPublicIdAsync(publicId, ct)
                ?? throw new NotFoundException("Persona", publicId);
    }
}
