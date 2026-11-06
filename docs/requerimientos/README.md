# Requerimientos — RxBox

Cada módulo tiene su archivo con los requerimientos funcionales (RF) en el formato de la plantilla de abajo.
El código paso a paso de cada módulo está en `docs/implementacion/` con el mismo número.

Alcance del MVP: **membresías, pagos y asistencia**. Esquema: `docs/esquema-v2.md`.

## Etapas de cada módulo

Cada módulo de mantenimiento se construye en este orden:

1. **Listado** (con filtros)
2. **Agregar**
3. **Editar**
4. **Eliminar lógicamente** (activar / desactivar con `PATCH .../toggle`)

Los módulos de operación (membresías, pagos, inscripciones) tienen sus propias etapas, que se definen en su archivo.

## Módulos

| # | Módulo | Prefijo RF | Estado |
|---|---|---|---|
| 00 | Transversal: bitácora, eventos de membresía | RF-TRV | Por construir. El esquema los define pero la migración inicial no creó `bitacora`, `membresia_eventos` ni `membresias.membresia_anterior_id`. Debe quedar antes del módulo 04. |
| 01 | [Catálogos](01-catalogos.md) (fijos y editables) | RF-CAF / RF-CAT | Definido |
| 02 | Personas (atletas, coaches) y contactos de emergencia | RF-PER | Por definir |
| 03 | Tipos de membresía y restricciones | RF-TME | Por definir |
| 04 | Membresías (venta, renovación, extensión, cancelación) | RF-MEM | Por definir |
| 05 | Pagos | RF-PAG | Por definir |
| 06 | Horarios y clases | RF-CLA | Por definir |
| 07 | Inscripciones y check-in | RF-INS | Por definir |
| 08 | Usuarios del box (accesos y roles) | RF-USU | Por definir |
| 09 | Métricas de asistencia e ingresos | RF-MET | Por definir |

Estados: Construido / Por ajustar / Por construir / Definido, sin fecha / Por definir.

## Convenciones

- **Actores**: `Admin` (rol ADMIN del box), `Coach` (rol COACH del box), `Atleta` (sin login; usa su código). "Usuario autenticado" = Admin o Coach.
- **Tenant**: todo requerimiento opera dentro del box del token. No se repite como regla en cada RF.
- **Identificadores**: las entradas y salidas usan `publicId` (uuid). El id interno nunca sale de la API.
- **Errores**: 400 validación de formato, 404 no existe (o es de otro box), 409 regla de negocio, 401 sin sesión, 403 sin permiso.

## Plantilla para nuevos requerimientos

Copia esta sección debajo del último requerimiento y reemplaza lo que está entre corchetes.

```
### RF-XXX-000: [Nombre del requerimiento]
**Estado:** [Construido / Por ajustar / Por construir / Definido, sin fecha]

El sistema deberá [qué hace].

**Actores**
- [Actor]

**Entradas**
- [Dato de entrada]

**Proceso**
1. El sistema [paso].

**Salida**
- [Resultado]

**Criterios de aceptación**
| ID | Definición criterio |
|---|---|
| CA-001 | Dado que [situación], cuando [acción], entonces el sistema deberá [resultado]. |

**Reglas de negocio**
| ID | Definición regla |
|---|---|
| RN-001 | [Regla] |

**Consideraciones funcionales**
- [Consideración]
```
