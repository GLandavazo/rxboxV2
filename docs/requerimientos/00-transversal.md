# Módulo 00 — Transversal: bitácora e historial de membresías

Dos registros con propósitos distintos:

- **Bitácora técnica (`bitacora`).** Quién cambió qué, cuándo y de qué valor a qué valor, en todas las tablas. Se llena sola desde el interceptor de EF. Incluye los accesos al sistema.
- **Historial de membresías (`membresia_eventos`).** Eventos de negocio legibles ("Renovó Mensual General…"). En este módulo solo se crea la estructura; se llena en el módulo 04.

Implementación: `docs/implementacion/00-transversal.md`.

---

### RF-TRV-001: Registro automático de cambios
**Estado:** Por construir

El sistema deberá registrar en la bitácora cada alta, cambio y borrado de datos, sin que cada servicio tenga que hacerlo.

**Actores**
- Sistema (cualquier operación que guarde datos)

**Entradas**
- Las entidades creadas, modificadas o borradas en un guardado

**Proceso**
1. Al guardar, el sistema identificará cada registro creado, modificado o borrado.
2. Por cada uno armará la lista de campos con su valor anterior y nuevo:
   - Alta: todos los campos con valor, anterior vacío.
   - Cambio: solo los campos cuyo valor cambió.
   - Borrado: todos los campos, nuevo vacío.
3. Guardará una fila de bitácora por registro, en la misma transacción que el cambio.

**Salida**
- Filas en `bitacora` con box, usuario, entidad, public_id, acción, cambios, fecha e IP.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado que se crea una persona, cuando se guarde, entonces el sistema deberá registrar una fila CREAR con su public_id y sus campos. |
| CA-002 | Dado que se cambia el nombre de una persona de "Ana" a "Ana María", cuando se guarde, entonces el sistema deberá registrar una fila EDITAR con `{"nombre": {"antes": "Ana", "despues": "Ana María"}}` y ningún otro campo. |
| CA-003 | Dado que se borra un contacto de emergencia, cuando se guarde, entonces el sistema deberá registrar una fila ELIMINAR con los valores que tenía. |
| CA-004 | Dado que se marca un registro como modificado sin cambiar valores, cuando se guarde, entonces el sistema no deberá registrar nada. |
| CA-005 | Dado que se crea un usuario, cuando se guarde, entonces la bitácora no deberá contener `password_hash`. |
| CA-006 | Dado que el guardado falla, cuando se haga rollback, entonces tampoco deberá quedar la fila de bitácora. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | Nunca se registran `password_hash` ni `token_hash`. |
| RN-002 | No se registran las tablas `bitacora`, `refresh_tokens` ni `membresia_eventos`. |
| RN-003 | Los campos de auditoría (`creado_por`, `fecha_creacion`, `editado_por`, `fecha_edicion`), `id`, `public_id` y `box_id` no van en `cambios`: ya están en la fila de bitácora o en el registro. |
| RN-004 | La bitácora es solo de inserción: nadie la edita ni la borra desde la aplicación. |
| RN-005 | No tiene llaves foráneas hacia lo que registra, para sobrevivir a borrados. |

**Consideraciones funcionales**
- Las operaciones con `ExecuteUpdate` no pasan por el interceptor y no se registran. Hoy solo las usa Auth (último login, revocar tokens), que no son ediciones de datos.
- Desactivar un registro (`activo = false`) es un EDITAR, no un ELIMINAR.

---

### RF-TRV-002: Registro de accesos
**Estado:** Por construir

El sistema deberá registrar los inicios de sesión exitosos y fallidos, y los cambios de box.

**Actores**
- Sistema (flujo de autenticación)

**Entradas**
- Email del intento, usuario y box resultantes

**Proceso**
1. Cuando se emitan tokens para un box por login o selección de box, registrará LOGIN con el usuario y el box.
2. Cuando las credenciales sean incorrectas, registrará LOGIN_FALLIDO con el email intentado y, si el email existe, el usuario.
3. Cuando un usuario cambie de box, registrará CAMBIO_BOX con el box nuevo.

**Salida**
- Filas en `bitacora` con entidad `Usuario` y la acción correspondiente.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado un usuario con un box, cuando inicie sesión correctamente, entonces el sistema deberá registrar LOGIN con su usuario y su box. |
| CA-002 | Dado un usuario con dos boxes, cuando inicie sesión, entonces el sistema no deberá registrar LOGIN hasta que seleccione el box. |
| CA-003 | Dado un password incorrecto, cuando intente iniciar sesión, entonces el sistema deberá registrar LOGIN_FALLIDO con el email y el usuario. |
| CA-004 | Dado un email que no existe, cuando intente iniciar sesión, entonces el sistema deberá registrar LOGIN_FALLIDO con el email y sin usuario. |
| CA-005 | Dado que un usuario cambia de box, cuando se emitan los tokens nuevos, entonces el sistema deberá registrar CAMBIO_BOX con el box nuevo. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | Renovar el token (refresh) no se registra: no es un acceso nuevo. |
| RN-002 | El LOGIN_FALLIDO no lleva box: todavía no se sabe a cuál entraría. |

---

### RF-TRV-003: Consulta de bitácora
**Estado:** Por construir

El sistema deberá permitir al Admin consultar la bitácora de su box para saber quién hizo un cambio.

**Actores**
- Admin

**Entradas**
- Entidad (opcional, ej. `Persona`)
- public_id del registro (opcional, para ver el historial de uno)
- Acción (opcional)
- Desde / hasta (opcionales, fechas en la zona horaria del box)
- Página y tamaño (por defecto 1 y 20, máximo 100)

**Proceso**
1. El sistema filtrará por el box del usuario y los filtros recibidos.
2. Ordenará del más reciente al más antiguo.
3. Paginará.

**Salida**
- `{ items: [{ fecha, accion, entidad, entidadPublicId, usuarioEmail, cambios, ip }], total, pagina, tamano, totalPaginas }`.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado un public_id, cuando se consulte, entonces el sistema deberá regresar solo el historial de ese registro, del más reciente al más antiguo. |
| CA-002 | Dado que otro box tiene registros, cuando se consulte, entonces el sistema no deberá regresarlos. |
| CA-003 | Dado un rango desde/hasta, cuando se consulte, entonces el sistema deberá incluir el día "hasta" completo según la zona horaria del box. |
| CA-004 | Dado que el usuario es Coach, cuando consulte, entonces el sistema deberá responder 403. |
| CA-005 | Dado un tamaño mayor a 100, cuando se consulte, entonces el sistema deberá responder 400. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | `cambios` se entrega como objeto JSON, no como texto. |

---

### RF-TRV-004: Estructura de historial de membresías y renovaciones
**Estado:** Por construir (solo estructura; se usa en el módulo 04)

El sistema deberá tener lista la estructura para guardar los eventos de cada membresía y encadenar sus renovaciones.

**Proceso**
1. Crear la tabla `membresia_eventos` (ALTA, RENOVACION, PAGO, PAGO_ANULADO, EXTENSION, CANCELACION).
2. Agregar `membresias.membresia_anterior_id` para que una renovación apunte a la membresía que renueva.

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | Una membresía solo puede renovarse una vez: `membresia_anterior_id` es único. |
| RN-002 | Los eventos los escribe el servicio de membresías y pagos, no el interceptor. |
