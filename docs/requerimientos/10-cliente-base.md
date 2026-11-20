# Módulo 10 — Cliente base (front)

Primera versión del panel web para Admin y Coach. No agrega reglas de negocio: consume las que ya existen en los módulos 00, 01 y 02. Lo único nuevo del lado del backend es la consulta de la sesión actual (RF-AUT-001), que el front necesita para mostrar el box y permitir cambiarlo.

Implementación: `docs/implementacion/10-cliente-base.md`.

## Pantallas y requerimientos que cubren

| Pantalla | Ruta | Rol | Requerimientos |
|---|---|---|---|
| Login | `/login` | Público | Auth (login, refresh, logout) |
| Elegir box | `/seleccionar-box` | Público con selección pendiente | Auth (seleccionar box) |
| Layout: box actual, cambio de box, menú, salir | todas | Admin, Coach | RF-AUT-001, Auth (cambiar box) |
| Inicio | `/` | Admin, Coach | — (placeholder hasta Métricas) |
| Catálogos | `/catalogos` | Admin | RF-CAF-001, RF-CAT-001 a 004 |
| Personas | `/personas` | Admin (edita), Coach (consulta) | RF-PER-001 a 004 |
| Detalle de persona | `/personas/:publicId` | Admin (edita), Coach (consulta) | RF-PER-001, RF-CEM-001 a 003 |

## Reglas del cliente

| ID | Regla |
|---|---|
| RC-001 | El Coach no ve botones de agregar, editar ni desactivar. El backend ya responde 403; ocultarlos solo evita confusión. |
| RC-002 | Si el access token expira, el cliente lo renueva una vez con el refresh token y repite la petición. Si el refresh falla, cierra la sesión y manda a `/login`. |
| RC-003 | Los errores 400, 403, 404 y 409 se muestran como aviso con el mensaje del backend (`detail`, o la lista de errores de validación). |
| RC-004 | Desactivar una persona o un elemento de catálogo pide confirmación. |
| RC-005 | Email y teléfono vacíos se mandan como `null`, no como texto vacío. |
| RC-006 | Los selects de valores fijos se llenan desde `/api/catalogos-fijos`, no con textos escritos en el front. |

---

### RF-AUT-001: Consulta de la sesión actual
**Estado:** Por construir

El sistema deberá regresar los datos de la sesión del usuario autenticado para que el front muestre su box actual y los boxes a los que puede cambiar.

**Actores**
- Usuario autenticado (Admin o Coach)

**Entradas**
- Ninguna (se toma del token)

**Proceso**
1. El sistema tomará el usuario y el box del token.
2. Buscará los boxes activos a los que el usuario tiene acceso activo.
3. Si el box del token ya no está entre ellos, responderá 401.

**Salida**
- `{ email, box: { publicId, nombre, rol }, boxes: [{ publicId, nombre, rol }] }`

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado un usuario con acceso a dos boxes que entró al box B, cuando consulte su sesión, entonces el sistema deberá regresar B como box actual y los dos boxes en la lista. |
| CA-002 | Dado un usuario cuyo acceso al box actual fue desactivado, cuando consulte su sesión, entonces el sistema deberá responder 401. |
| CA-003 | Dado que no hay token, cuando se consulte, entonces el sistema deberá responder 401. |
