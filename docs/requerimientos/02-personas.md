# Módulo 02 — Personas y contactos de emergencia

Una **persona** es alguien del box: atleta, coach o ambos. Cada box tiene sus propias personas; la misma persona real en dos boxes son dos registros. Los atletas no tienen login: se identifican con su **código**.

Cada persona puede tener hasta tres **contactos de emergencia**.

Implementación: `docs/implementacion/02-personas.md`.

> Decisiones tomadas para este documento, a confirmar:
> - Solo el Admin agrega, edita y desactiva personas y contactos. El Coach solo consulta, incluidos los contactos de emergencia.
> - El código del atleta es de 6 dígitos al azar, lo genera el sistema y no se edita.
> - Teléfono: 10 dígitos (México).
> - Máximo 3 contactos de emergencia por persona.

---

## 1. Personas

### RF-PER-001: Listado y detalle de personas
**Estado:** Por construir

El sistema deberá listar las personas del box con búsqueda, filtros y paginación, y mostrar el detalle de una con sus contactos de emergencia.

**Actores**
- Usuario autenticado (Admin o Coach)

**Entradas**
- Buscar (opcional): texto en nombre completo, código, email o teléfono
- Tipo (opcional): `ATLETA` o `COACH`
- Solo activos (opcional, por defecto sí)
- Página y tamaño (por defecto 1 y 20, máximo 100)
- Para el detalle: publicId

**Proceso**
1. El sistema filtrará por el box del usuario.
2. Si viene buscar, regresará las personas cuyo nombre completo, código, email o teléfono contengan el texto, sin distinguir mayúsculas.
3. Si viene tipo, regresará solo atletas o solo coaches. Una persona que es ambos aparece en los dos.
4. Ordenará por apellidos y nombre, y paginará.
5. En el detalle regresará además sus contactos de emergencia.

**Salida**
- Listado: `{ items: [{ publicId, codigo, nombre, apellidos, email, telefono, esAtleta, esCoach, activo }], total, pagina, tamano, totalPaginas }`.
- Detalle: lo mismo más `fechaCreacion` y `contactos: [{ publicId, nombre, parentesco, telefono }]`.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado "ana l", cuando se busque, entonces el sistema deberá encontrar a "Ana López" (busca en nombre y apellidos juntos). |
| CA-002 | Dado el código de una persona, cuando se busque, entonces el sistema deberá encontrarla. |
| CA-003 | Dado tipo COACH, cuando se consulte, entonces el sistema deberá regresar solo personas con `esCoach` verdadero. |
| CA-004 | Dado tamaño 1 y dos resultados, cuando se consulte, entonces el sistema deberá regresar un elemento, `total` 2 y `totalPaginas` 2. |
| CA-005 | Dado que solo activos es verdadero o no se envía, cuando se consulte, entonces el sistema no deberá regresar personas inactivas. |
| CA-006 | Dado un publicId de otro box, cuando se pida el detalle, entonces el sistema deberá responder 404. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | El listado no muestra contactos de emergencia; solo el detalle. |

**Consideraciones funcionales**
- El estado de membresía (vigente, por vencer, vencida) se agregará a este listado en el módulo 04.
- La búsqueda distingue acentos: "lopez" no encuentra "López". Se puede resolver después con la extensión `unaccent` de Postgres.

---

### RF-PER-002: Agregar persona
**Estado:** Por construir

El sistema deberá permitir al Admin registrar una persona, opcionalmente con sus contactos de emergencia, y le asignará un código.

**Actores**
- Admin

**Entradas**
- Nombre (obligatorio, máximo 80)
- Apellidos (obligatorio, máximo 120)
- Email (opcional, formato válido, máximo 150)
- Teléfono (opcional, 10 dígitos)
- Es atleta (por defecto sí) / Es coach (por defecto no)
- Contactos de emergencia (opcional, máximo 3; cada uno con nombre, parentesco opcional y teléfono)

**Proceso**
1. El sistema limpiará nombre y apellidos (espacios sobrantes) y guardará el email en minúsculas.
2. Validará que la persona sea atleta, coach o ambos.
3. Generará un código de 6 dígitos que no exista en el box.
4. Guardará la persona activa y sus contactos en una sola operación.

**Salida**
- El detalle de la persona creada, con 201.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado nombre y apellidos válidos, cuando se agregue, entonces el sistema deberá crear la persona activa con un código de 6 dígitos y responder 201. |
| CA-002 | Dado que no es atleta ni coach, cuando se agregue, entonces el sistema deberá responder 400. |
| CA-003 | Dado un teléfono que no tiene 10 dígitos, cuando se agregue, entonces el sistema deberá responder 400. |
| CA-004 | Dados 4 contactos de emergencia, cuando se agregue, entonces el sistema deberá responder 400. |
| CA-005 | Dados 2 contactos válidos, cuando se agregue, entonces el sistema deberá guardar la persona y los 2 contactos; si algo falla, no deberá guardar nada. |
| CA-006 | Dado que el usuario es Coach, cuando intente agregar, entonces el sistema deberá responder 403. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | El código es único por box, de 6 dígitos al azar (no consecutivo, para que no se adivine el de otro atleta). Lo genera el sistema y no se edita. |
| RN-002 | No se valida duplicado por nombre ni por email: familias comparten correo y hay homónimos. El código es el identificador. |
| RN-003 | Las personas que solo administran (sin entrenar ni dar clases) se dan de alta desde Usuarios (módulo 08). |

**Consideraciones funcionales**
- El front manda `null` (o no manda el campo) cuando email o teléfono vienen vacíos.

---

### RF-PER-003: Editar persona
**Estado:** Por construir

El sistema deberá permitir al Admin corregir los datos de una persona.

**Actores**
- Admin

**Entradas**
- publicId
- Nombre, apellidos, email, teléfono, es atleta, es coach (mismas reglas que al agregar)

**Proceso**
1. El sistema buscará la persona en el box.
2. Aplicará las mismas validaciones y limpieza que al agregar.
3. Guardará los cambios.

**Salida**
- El detalle actualizado.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dada una persona, cuando se edite, entonces el sistema deberá guardar los cambios sin modificar su código. |
| CA-002 | Dado que se desmarcan atleta y coach, cuando se edite, entonces el sistema deberá responder 400. |
| CA-003 | Dado un publicId de otro box, cuando se edite, entonces el sistema deberá responder 404. |
| CA-004 | Dado que el usuario es Coach, cuando intente editar, entonces el sistema deberá responder 403. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | El código no se modifica. |
| RN-002 | Cuando exista el módulo 06: no se podrá quitar "coach" a quien tenga horarios activos asignados. |

**Consideraciones funcionales**
- Los contactos se editan por separado (sección 2).

---

### RF-PER-004: Activar o desactivar persona
**Estado:** Por construir

El sistema deberá permitir al Admin dar de baja a una persona sin borrarla, y reactivarla.

**Actores**
- Admin

**Entradas**
- publicId

**Proceso**
1. El sistema buscará la persona en el box.
2. Si está activa y tiene acceso al panel (usuario del box):
   - validará que no sea el propio usuario,
   - validará que no sea el único Admin activo del box,
   - le quitará el acceso a ese box y cerrará sus sesiones abiertas en él.
3. Cambiará su estado.

**Salida**
- La persona con su nuevo estado.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dada una persona activa sin acceso al panel, cuando se cambie su estado, entonces el sistema deberá desactivarla. |
| CA-002 | Dada una persona inactiva, cuando se cambie su estado, entonces el sistema deberá activarla. |
| CA-003 | Dada una persona con acceso al panel, cuando se desactive, entonces el sistema deberá desactivar su acceso a ese box y revocar sus refresh tokens de ese box. |
| CA-004 | Dado que el Admin intenta desactivar su propia persona, cuando se cambie el estado, entonces el sistema deberá responder 409. |
| CA-005 | Dado que la persona es el único Admin activo del box, cuando se intente desactivar, entonces el sistema deberá responder 409. |
| CA-006 | Dada una persona desactivada y reactivada, cuando se reactive, entonces el sistema no deberá devolverle el acceso al panel. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | Nunca se borra físicamente: membresías, pagos e inscripciones apuntan a la persona. |
| RN-002 | El acceso al panel se devuelve desde Usuarios (módulo 08), no al reactivar. |
| RN-003 | Cuando exista el módulo 04: desactivar no cancela membresías vigentes; eso se decide en Membresías. |

**Consideraciones funcionales**
- El access token de quien pierde el acceso sigue válido hasta 15 minutos; no puede renovarse.

---

## 2. Contactos de emergencia

Se consultan dentro del detalle de la persona (RF-PER-001).

### RF-CEM-001: Agregar contacto de emergencia
**Estado:** Por construir

**Actores**
- Admin

**Entradas**
- publicId de la persona
- Nombre (obligatorio, máximo 120), parentesco (opcional, máximo 40), teléfono (obligatorio, 10 dígitos)

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dada una persona con menos de 3 contactos, cuando se agregue uno válido, entonces el sistema deberá guardarlo y responder 201. |
| CA-002 | Dada una persona con 3 contactos, cuando se agregue otro, entonces el sistema deberá responder 409. |
| CA-003 | Dado un contacto sin teléfono o con teléfono de otro largo, cuando se agregue, entonces el sistema deberá responder 400. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | Máximo 3 contactos por persona. |

### RF-CEM-002: Editar contacto de emergencia
**Estado:** Por construir

**Actores**
- Admin

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado un contacto de la persona, cuando se edite, entonces el sistema deberá guardar nombre, parentesco y teléfono. |
| CA-002 | Dado un contacto que no pertenece a esa persona, cuando se edite, entonces el sistema deberá responder 404. |

### RF-CEM-003: Eliminar contacto de emergencia
**Estado:** Por construir

**Actores**
- Admin

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado un contacto de la persona, cuando se elimine, entonces el sistema deberá borrarlo y responder 204. |
| CA-002 | Dado un contacto eliminado, cuando se consulte la bitácora, entonces deberá aparecer el registro ELIMINAR con sus datos. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | El contacto sí se borra físicamente: nada lo referencia y la bitácora conserva lo que tenía. |
