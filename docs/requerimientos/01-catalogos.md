# Módulo 01 — Catálogos

Dos tipos de catálogo:

- **Fijos (del sistema).** Sus valores los usa el código para decidir algo (tipo de cobertura, estado de una inscripción…). Viven como enum en C# y se guardan como texto con un `CHECK` en la BD. No tienen pantalla de mantenimiento. Si cambian, se cambia el enum y se hace migración.
- **Editables (por box).** Cada box los administra desde su panel: hoy son **categorías de membresía** y **métodos de pago**. Viven en la tabla `catalogos`.

Implementación: `docs/implementacion/01-catalogos.md`.

---

## 1. Catálogos fijos

### RF-CAF-001: Consulta de catálogos fijos para selects
**Estado:** Definido, sin fecha

El sistema deberá entregar los valores de un catálogo fijo para llenar listas desplegables.

**Actores**
- Usuario autenticado (Admin o Coach)

**Entradas**
- Nombre del catálogo (opcional)

**Proceso**
1. Si no viene nombre, el sistema regresará los nombres de catálogo disponibles.
2. Si viene un nombre válido, el sistema regresará sus valores en el orden en que están declarados.

**Salida**
- Lista de nombres de catálogo, o lista de `{ valor, descripcion }`.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado que se consulta sin nombre, cuando se responda, entonces el sistema deberá regresar los nombres disponibles. |
| CA-002 | Dado un nombre válido, cuando se consulte, entonces el sistema deberá regresar sus valores en orden de declaración, cada uno con su descripción legible. |
| CA-003 | Dado un nombre que no existe, cuando se consulte, entonces el sistema deberá responder 404. |
| CA-004 | Dado que no hay sesión, cuando se consulte, entonces el sistema deberá responder 401. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | Los catálogos disponibles son: `tipos-cobertura`, `expira-desde`, `estados-clase`, `estados-inscripcion`, `canales-reserva`, `canales-checkin`, `roles-usuario`, `tipos-catalogo`. |
| RN-002 | No se dan de alta ni se editan desde la aplicación. |
| RN-003 | El `valor` es exactamente el que se guarda en BD y el que se manda en las peticiones (ej. `PRIMERA_VISITA`). La `descripcion` es solo para mostrar (ej. "Primera visita"). |
| RN-004 | En C# se usan siempre por su enum, nunca como texto suelto. |

**Consideraciones funcionales**
- No hay pantalla: el front lo consume solo para llenar selects.
- Para agregar otro catálogo fijo basta una línea en el diccionario de `CatalogoFijoService`.

---

## 2. Catálogos editables

Tipos: `CATEGORIA_MEMBRESIA`, `METODO_PAGO`. Al crear un box se siembran General y Estudiante, y Efectivo, Transferencia y Tarjeta.

### RF-CAT-001: Listado de catálogos editables
**Estado:** Definido, sin fecha

El sistema deberá listar los elementos de los catálogos editables del box, para la pantalla de mantenimiento y para llenar selects.

**Actores**
- Usuario autenticado (Admin o Coach)

**Entradas**
- Tipo de catálogo (opcional)
- Solo activos (opcional, por defecto sí)

**Proceso**
1. El sistema filtrará por el box del usuario.
2. Si viene tipo, filtrará por ese tipo.
3. Si solo activos es verdadero, omitirá los inactivos.
4. Ordenará por tipo, orden y nombre.

**Salida**
- Lista de `{ publicId, tipo, nombre, orden, activo }`.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado un tipo, cuando se consulte, entonces el sistema deberá regresar solo elementos de ese tipo, ordenados por orden y nombre. |
| CA-002 | Dado que solo activos es verdadero o no se envía, cuando se consulte, entonces el sistema no deberá regresar inactivos. |
| CA-003 | Dado que solo activos es falso, cuando se consulte, entonces el sistema deberá regresar activos e inactivos. |
| CA-004 | Dado un tipo que no existe, cuando se consulte, entonces el sistema deberá responder 400. |
| CA-005 | Dado que otro box tiene elementos, cuando se consulte, entonces el sistema no deberá regresarlos. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | Los selects del front piden siempre solo activos. La pantalla de mantenimiento pide todos. |

**Consideraciones funcionales**
- Sin paginación: son listas cortas.
- Endpoint de detalle por `publicId` para la pantalla de edición (404 si no existe o es de otro box).

---

### RF-CAT-002: Agregar elemento a un catálogo editable
**Estado:** Definido, sin fecha

El sistema deberá permitir al Admin agregar un elemento a un catálogo editable de su box.

**Actores**
- Admin

**Entradas**
- Tipo (obligatorio)
- Nombre (obligatorio, máximo 60 caracteres)
- Orden (opcional)

**Proceso**
1. El sistema limpiará el nombre: quita espacios al inicio y final y deja un solo espacio entre palabras.
2. Validará que no exista otro elemento del mismo tipo con el mismo nombre, sin distinguir mayúsculas.
3. Si no viene orden, asignará el siguiente al mayor del tipo.
4. Guardará el elemento como activo.

**Salida**
- El elemento creado `{ publicId, tipo, nombre, orden, activo }`, con 201.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado un nombre nuevo, cuando se agregue, entonces el sistema deberá crearlo activo y responder 201. |
| CA-002 | Dado que no se envía orden, cuando se agregue, entonces el sistema deberá asignar el mayor orden del tipo más uno (1 si es el primero). |
| CA-003 | Dado que ya existe "Efectivo" activo, cuando se agregue " efectivo ", entonces el sistema deberá responder 409 indicando que ya existe. |
| CA-004 | Dado que ya existe "Efectivo" inactivo, cuando se agregue "Efectivo", entonces el sistema deberá responder 409 indicando que está inactivo y que se reactive. |
| CA-005 | Dado un nombre vacío, de solo espacios o de más de 60 caracteres, cuando se agregue, entonces el sistema deberá responder 400. |
| CA-006 | Dado que el usuario es Coach, cuando intente agregar, entonces el sistema deberá responder 403. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | El nombre es único por box y tipo, sin distinguir mayúsculas, incluyendo inactivos. |
| RN-002 | Solo el Admin da de alta. |

**Consideraciones funcionales**
- Si se repite un inactivo, el mensaje guía a reactivarlo en lugar de crear un duplicado.

---

### RF-CAT-003: Editar elemento de un catálogo editable
**Estado:** Definido, sin fecha

El sistema deberá permitir al Admin cambiar el nombre y el orden de un elemento.

**Actores**
- Admin

**Entradas**
- publicId del elemento
- Nombre (obligatorio, máximo 60 caracteres)
- Orden (opcional; si no viene, se conserva)

**Proceso**
1. El sistema buscará el elemento en el box del usuario.
2. Limpiará el nombre y validará que no lo use otro elemento del mismo tipo.
3. Guardará los cambios.

**Salida**
- El elemento actualizado.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado un elemento existente, cuando se edite con un nombre libre, entonces el sistema deberá guardar y regresar el elemento actualizado. |
| CA-002 | Dado un elemento, cuando se edite con el nombre de otro elemento del mismo tipo, entonces el sistema deberá responder 409. |
| CA-003 | Dado un elemento "Efectivo", cuando se edite a "EFECTIVO", entonces el sistema deberá permitirlo (es el mismo elemento). |
| CA-004 | Dado un publicId que no existe o es de otro box, cuando se edite, entonces el sistema deberá responder 404. |
| CA-005 | Dado que el usuario es Coach, cuando intente editar, entonces el sistema deberá responder 403. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | El tipo no se puede cambiar después de crear el elemento. |
| RN-002 | Cambiar el nombre no altera registros históricos: las membresías guardan el nombre de la categoría como snapshot. |

**Consideraciones funcionales**
- Un pago apunta al método de pago por id, así que al renombrar un método los pagos anteriores muestran el nombre nuevo. Es lo esperado para corregir errores de escritura; si se quiere un método distinto, se crea otro.

---

### RF-CAT-004: Activar o desactivar elemento (eliminación lógica)
**Estado:** Definido, sin fecha

El sistema deberá permitir al Admin desactivar un elemento para que ya no aparezca en selects, y reactivarlo después.

**Actores**
- Admin

**Entradas**
- publicId del elemento

**Proceso**
1. El sistema buscará el elemento en el box del usuario.
2. Si está activo, validará que no sea el último activo de su tipo y lo desactivará.
3. Si está inactivo, lo activará.

**Salida**
- El elemento con su nuevo estado.

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado un elemento activo con otros activos de su tipo, cuando se cambie su estado, entonces el sistema deberá desactivarlo. |
| CA-002 | Dado un elemento inactivo, cuando se cambie su estado, entonces el sistema deberá activarlo. |
| CA-003 | Dado que es el único método de pago activo, cuando se intente desactivar, entonces el sistema deberá responder 409. |
| CA-004 | Dado un elemento desactivado, cuando se liste con solo activos, entonces el sistema no deberá regresarlo. |
| CA-005 | Dado un publicId que no existe o es de otro box, cuando se cambie su estado, entonces el sistema deberá responder 404. |
| CA-006 | Dado que el usuario es Coach, cuando intente cambiar el estado, entonces el sistema deberá responder 403. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | Nunca se borra físicamente: tipos de membresía y pagos pueden apuntar al elemento. |
| RN-002 | Debe quedar al menos un elemento activo por tipo; sin método de pago activo no se podría registrar un pago. |
| RN-003 | Desactivar no afecta registros existentes: los pagos ya hechos con ese método lo conservan. |
