# RxBox v2 — Esquema de base de datos (MVP)

Alcance: membresías, pagos y asistencia. Fuera por ahora: WODs, programas, ajustes de membresía.
Diagrama: `esquema-v2.png`.

## Reglas comunes

- **PK** `id int identity` interna. **`public_id uuid`** (`Guid.CreateVersion7()`) con UQ: es lo único que sale en DTOs y rutas. Excepción: tablas hijas sin `public_id` (ver abajo).
- **Tenant**: toda tabla de negocio lleva `box_id`. Query filter global por `box_id` del usuario actual. El interceptor asigna `box_id` al insertar.
- **FK compuestas** `(box_id, x_id) → (box_id, id)` entre tablas tenant (el padre declara alternate key `(box_id, id)`). Así es imposible referenciar filas de otro box.
- **Índices** con `box_id` primero.
- **Auditoría** (`creado_por int?`, `fecha_creacion`, `editado_por int?`, `fecha_edicion`) en todas salvo `refresh_tokens`, `clase_programacion_dias`, `tipo_membresia_restricciones`. `creado_por`/`editado_por` = `usuarios.id`.
- **Dinero** `numeric(12,2)`. **Fecha-hora** `timestamptz`. **Hora del día** `time`. **Fecha** `date`.
- **Enums de sistema**: enum C# guardado como `text` + `CHECK`. **Catálogos editables por box**: tabla `catalogos`.
- **Estados calculados, no persistidos**: membresía pagada (suma de pagos no anulados ≥ monto), vigente/vencida/por vencer, visitas usadas (inscripciones PRESENTE con esa membresía).
- **Borrado lógico** (`activo`) solo en catálogos, tipos de membresía, personas, programaciones, usuarios. Pagos e inscripciones no se borran.

## Tablas

### boxes (no tenant)
id, public_id, nombre, slug (UQ, inmutable), zona_horaria (default `America/Hermosillo`), ventana_checkin_minutos int, ventana_reserva_dias int, logo_url?, color_primario?, activo, auditoría.

### personas
id, public_id, box_id, codigo text, nombre, apellidos, email?, telefono?, es_atleta bool, es_coach bool, activo, auditoría.
- UQ(box_id, codigo). Índice (box_id, apellidos, nombre).
- `codigo` lo generará el servicio de personas (fuera de este paso).

### contactos_emergencia
id, public_id, box_id, persona_id, nombre (req), parentesco?, telefono (req), auditoría. 1:N con personas.

### usuarios (global, no tenant)
id, public_id, email, password_hash, ultimo_login?, activo, auditoría.
- UQ sobre `lower(email)`.

### usuario_boxes (lleva box_id, sin query filter: se consulta en login)
id, usuario_id, box_id, persona_id, rol (`ADMIN | COACH`), activo, auditoría.
- UQ(usuario_id, box_id). FK (box_id, persona_id) → personas.

### refresh_tokens (sin query filter)
id, usuario_id, box_id, token_hash (UQ, SHA-256), expira_en, revocado_en?, created_at.

### catalogos
id, public_id, box_id, tipo (`CATEGORIA_MEMBRESIA | METODO_PAGO`), nombre, orden smallint, activo, auditoría.
- UQ(box_id, tipo, nombre).
- Al crear un box se siembran: CATEGORIA_MEMBRESIA (General, Estudiante), METODO_PAGO (Efectivo, Transferencia, Tarjeta).

### tipos_membresia
id, public_id, box_id, nombre, categoria_id? (→ catalogos CATEGORIA_MEMBRESIA), tipo_cobertura (`ILIMITADA | PAQUETE | VISITA`), monto, duracion_dias int, visitas_incluidas int?, expira_desde (`COMPRA | PRIMERA_VISITA`), max_personas smallint default 1, es_promocion bool, descripcion_promo?, fecha_caducidad date?, activo, auditoría.
- CHECK monto ≥ 0, duracion_dias > 0, max_personas ≥ 1. CHECK visitas_incluidas NOT NULL cuando tipo_cobertura ≠ ILIMITADA.
- UQ(box_id, nombre).

### tipo_membresia_restricciones (hija, sin public_id)
id, box_id, tipo_membresia_id, programacion_id?, hora_inicio time?, hora_fin time?, dias_semana smallint[]?.
- CHECK: (programacion_id NOT NULL y horas/días NULL) **o** (programacion_id NULL y hora_inicio, hora_fin NOT NULL y hora_fin > hora_inicio).
- CHECK días entre 1 y 7 (1 = lunes).
- Regla de negocio: sin filas = sin restricción; con filas, la clase debe cumplir al menos una. Clases `es_especial` ignoran restricciones. Se leen del tipo en el momento (no snapshot).

### membresias
id, public_id, box_id, persona_id, persona_vinculada_id?, tipo_membresia_id?,
snapshot: tipo_nombre, tipo_cobertura, categoria_nombre?, monto,
fecha_inicio date?, fecha_fin date? (nulas mientras expira_desde = PRIMERA_VISITA y no hay visita), visitas_totales int?, cancelada_en?, cancelada_por? (→ usuarios), notas?, auditoría.
- Índices (box_id, persona_id), (box_id, fecha_fin).

### pagos
id, public_id, box_id, membresia_id, persona_id, monto (CHECK > 0), metodo_pago_id (→ catalogos METODO_PAGO), fecha_pago date, registrado_por (→ usuarios), anulado_en?, notas?, auditoría.
- Insert-only: correcciones = anular + nuevo pago.
- Índices (box_id, fecha_pago), (box_id, membresia_id).

### clase_programaciones
id, public_id, box_id, nombre, hora_inicio, hora_fin, coach_id? (→ personas), cupo smallint (> 0), sobrecupo smallint (≥ 0), exige_reserva_checkin bool, vigente_desde date, vigente_hasta date?, activo, auditoría.
- CHECK hora_fin > hora_inicio.

### clase_programacion_dias (hija, sin public_id)
id, box_id, programacion_id, dia_semana smallint (1..7). UQ(programacion_id, dia_semana).

### clases
id, public_id, box_id, programacion_id?, fecha, nombre, hora_inicio, hora_fin, coach_id?, cupo, sobrecupo, sobrecupo_habilitado bool, es_especial bool, estado (`ACTIVA | CANCELADA`), notas?, auditoría.
- Índice (box_id, fecha). UQ parcial (programacion_id, fecha) WHERE programacion_id IS NOT NULL.

### inscripciones
id, public_id, box_id, clase_id, persona_id, membresia_id?, estado (`INSCRITO | PRESENTE | AUSENTE | CANCELADO`), canal_reserva (`COACH | PORTAL`), canal_checkin? (`COACH | PORTAL | QR`), fecha_reserva, fecha_checkin?, checkin_by? (→ usuarios), auditoría.
- UQ parcial (clase_id, persona_id) WHERE estado <> 'CANCELADO'.
- Índices (box_id, persona_id), (box_id, clase_id).

## Autenticación

- JWT propio (HS256, secreto por config/env). Access 15 min. Refresh 30 días, guardado como hash, con rotación.
- Claims: `sub` = usuarios.id, `box_id`, `rol`. (Ids internos solo dentro del token firmado; nunca en rutas ni respuestas.)
- Flujo:
  1. `POST /api/auth/login` {email, password}. Un box activo → tokens. Varios → `{ requiereSeleccion, boxes:[{publicId, nombre, rol}], selectionToken }` (token de 5 min, sin box).
  2. `POST /api/auth/seleccionar-box` {selectionToken, boxPublicId} → tokens para ese box.
  3. `POST /api/auth/cambiar-box` {boxPublicId} (autenticado) → tokens nuevos sin pedir contraseña.
  4. `POST /api/auth/refresh`, `POST /api/auth/logout` (revoca refresh).
- Hash de contraseña: `PasswordHasher<Usuario>` de ASP.NET Core.
- Atletas no tienen login.
