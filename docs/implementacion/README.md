# Implementación — RxBox

Código paso a paso para implementar cada módulo a mano. Cada archivo corresponde a un módulo de `docs/requerimientos/` con el mismo número y está escrito contra el código real de `server/` (no contra la plantilla original).

## Receta por etapa

Cada etapa (listado, agregar, editar, eliminar lógico) sigue los mismos pasos y termina compilando, con su prueba en Postman y su test.

1. **DTOs** en `Core/DTOs/Requests/<Modulo>/` y `Core/DTOs/Responses/<Modulo>/`. Records con DataAnnotations; `[ApiController]` responde 400 solo.
2. **Interfaz** del servicio en `Core/Interfaces/I<Modulo>Service.cs`.
3. **Servicio** en `Infrastructure/Services/<Modulo>/<Modulo>Service.cs`. Usa `IUnitOfWork`; las reglas de negocio lanzan `ConflictException` / `NotFoundException`, nunca `InvalidOperationException`.
4. **Registro** en `ServiceExtensions.AddAppServices`.
5. **Controller** en `API/Controllers/<Modulo>Controller.cs`. Delgado: recibe, llama al servicio, regresa.
6. **Postman**: la colección ya trae las peticiones de cada módulo en `docs/postman/`.
7. **Test** en `Tests/<Modulo>Tests.cs` usando `RxBoxApiFactory`.
8. **Commit**: `feat(<modulo>): <etapa>`.

## Convenciones que ya trae la base

- **IDs**: rutas y DTOs usan `PublicId` (`Guid`). `GetByPublicIdAsync` del repositorio genérico ya filtra por box.
- **Tenant**: el query filter de `AppDbContext` limita todo al box del token; el interceptor asigna `BoxId` y la auditoría al guardar. No se filtra por box a mano.
- **Roles**: `[Authorize(Roles = nameof(RolUsuario.ADMIN))]`. El claim de rol es `rol` y ya está mapeado como `RoleClaimType`.
- **Errores**: `GlobalExceptionHandler` traduce `ConflictException`/`DependencyException` → 409, `NotFoundException` → 404, `UnauthenticatedException` → 401.
- **Tracking**: `GetByPublicIdAsync` regresa la entidad rastreada; para editar basta cambiar propiedades y `uow.SaveAsync()`, sin `Update()`.
- **Listados**: `AsQueryable().AsNoTracking()` + `Select` directo al DTO de respuesta.

## Módulos

| # | Archivo | Estado |
|---|---|---|
| 01 | [01-catalogos.md](01-catalogos.md) | Listo para implementar |
