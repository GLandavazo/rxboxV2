using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RxBox.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "boxes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    zona_horaria = table.Column<string>(type: "text", nullable: false, defaultValue: "America/Hermosillo"),
                    ventana_checkin_minutos = table.Column<int>(type: "integer", nullable: false),
                    ventana_reserva_dias = table.Column<int>(type: "integer", nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    color_primario = table.Column<string>(type: "text", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boxes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    ultimo_login = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalogos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<short>(type: "smallint", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogos", x => x.id);
                    table.UniqueConstraint("AK_catalogos_box_id_id", x => new { x.box_id, x.id });
                    table.CheckConstraint("ck_catalogos_tipo", "tipo IN ('CATEGORIA_MEMBRESIA', 'METODO_PAGO')");
                    table.ForeignKey(
                        name: "FK_catalogos_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "personas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    apellidos = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    telefono = table.Column<string>(type: "text", nullable: true),
                    es_atleta = table.Column<bool>(type: "boolean", nullable: false),
                    es_coach = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personas", x => x.id);
                    table.UniqueConstraint("AK_personas_box_id_id", x => new { x.box_id, x.id });
                    table.ForeignKey(
                        name: "FK_personas_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revocado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tipos_membresia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    categoria_id = table.Column<int>(type: "integer", nullable: true),
                    tipo_cobertura = table.Column<string>(type: "text", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    duracion_dias = table.Column<int>(type: "integer", nullable: false),
                    visitas_incluidas = table.Column<int>(type: "integer", nullable: true),
                    expira_desde = table.Column<string>(type: "text", nullable: false),
                    max_personas = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    es_promocion = table.Column<bool>(type: "boolean", nullable: false),
                    descripcion_promo = table.Column<string>(type: "text", nullable: true),
                    fecha_caducidad = table.Column<DateOnly>(type: "date", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_membresia", x => x.id);
                    table.UniqueConstraint("AK_tipos_membresia_box_id_id", x => new { x.box_id, x.id });
                    table.CheckConstraint("ck_tipos_membresia_duracion_dias", "duracion_dias > 0");
                    table.CheckConstraint("ck_tipos_membresia_expira_desde", "expira_desde IN ('COMPRA', 'PRIMERA_VISITA')");
                    table.CheckConstraint("ck_tipos_membresia_max_personas", "max_personas >= 1");
                    table.CheckConstraint("ck_tipos_membresia_monto", "monto >= 0");
                    table.CheckConstraint("ck_tipos_membresia_tipo_cobertura", "tipo_cobertura IN ('ILIMITADA', 'PAQUETE', 'VISITA')");
                    table.CheckConstraint("ck_tipos_membresia_visitas_incluidas", "tipo_cobertura = 'ILIMITADA' OR visitas_incluidas IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_tipos_membresia_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tipos_membresia_catalogos_box_id_categoria_id",
                        columns: x => new { x.box_id, x.categoria_id },
                        principalTable: "catalogos",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clase_programaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    coach_id = table.Column<int>(type: "integer", nullable: true),
                    cupo = table.Column<short>(type: "smallint", nullable: false),
                    sobrecupo = table.Column<short>(type: "smallint", nullable: false),
                    exige_reserva_checkin = table.Column<bool>(type: "boolean", nullable: false),
                    vigente_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    vigente_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clase_programaciones", x => x.id);
                    table.UniqueConstraint("AK_clase_programaciones_box_id_id", x => new { x.box_id, x.id });
                    table.CheckConstraint("ck_clase_programaciones_cupo", "cupo > 0");
                    table.CheckConstraint("ck_clase_programaciones_horas", "hora_fin > hora_inicio");
                    table.CheckConstraint("ck_clase_programaciones_sobrecupo", "sobrecupo >= 0");
                    table.ForeignKey(
                        name: "FK_clase_programaciones_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_clase_programaciones_personas_box_id_coach_id",
                        columns: x => new { x.box_id, x.coach_id },
                        principalTable: "personas",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contactos_emergencia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    persona_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    parentesco = table.Column<string>(type: "text", nullable: true),
                    telefono = table.Column<string>(type: "text", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contactos_emergencia", x => x.id);
                    table.ForeignKey(
                        name: "FK_contactos_emergencia_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contactos_emergencia_personas_box_id_persona_id",
                        columns: x => new { x.box_id, x.persona_id },
                        principalTable: "personas",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario_boxes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    persona_id = table.Column<int>(type: "integer", nullable: false),
                    rol = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_boxes", x => x.id);
                    table.CheckConstraint("ck_usuario_boxes_rol", "rol IN ('ADMIN', 'COACH')");
                    table.ForeignKey(
                        name: "FK_usuario_boxes_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuario_boxes_personas_box_id_persona_id",
                        columns: x => new { x.box_id, x.persona_id },
                        principalTable: "personas",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuario_boxes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "membresias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    persona_id = table.Column<int>(type: "integer", nullable: false),
                    persona_vinculada_id = table.Column<int>(type: "integer", nullable: true),
                    tipo_membresia_id = table.Column<int>(type: "integer", nullable: true),
                    tipo_nombre = table.Column<string>(type: "text", nullable: false),
                    tipo_cobertura = table.Column<string>(type: "text", nullable: false),
                    categoria_nombre = table.Column<string>(type: "text", nullable: true),
                    monto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    visitas_totales = table.Column<int>(type: "integer", nullable: true),
                    cancelada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelada_por = table.Column<int>(type: "integer", nullable: true),
                    notas = table.Column<string>(type: "text", nullable: true),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membresias", x => x.id);
                    table.UniqueConstraint("AK_membresias_box_id_id", x => new { x.box_id, x.id });
                    table.CheckConstraint("ck_membresias_tipo_cobertura", "tipo_cobertura IN ('ILIMITADA', 'PAQUETE', 'VISITA')");
                    table.ForeignKey(
                        name: "FK_membresias_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membresias_personas_box_id_persona_id",
                        columns: x => new { x.box_id, x.persona_id },
                        principalTable: "personas",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membresias_personas_box_id_persona_vinculada_id",
                        columns: x => new { x.box_id, x.persona_vinculada_id },
                        principalTable: "personas",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membresias_tipos_membresia_box_id_tipo_membresia_id",
                        columns: x => new { x.box_id, x.tipo_membresia_id },
                        principalTable: "tipos_membresia",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membresias_usuarios_cancelada_por",
                        column: x => x.cancelada_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clase_programacion_dias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    programacion_id = table.Column<int>(type: "integer", nullable: false),
                    dia_semana = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clase_programacion_dias", x => x.id);
                    table.CheckConstraint("ck_clase_programacion_dias_dia_semana", "dia_semana BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_clase_programacion_dias_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_clase_programacion_dias_clase_programaciones_box_id_program~",
                        columns: x => new { x.box_id, x.programacion_id },
                        principalTable: "clase_programaciones",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    programacion_id = table.Column<int>(type: "integer", nullable: true),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    coach_id = table.Column<int>(type: "integer", nullable: true),
                    cupo = table.Column<short>(type: "smallint", nullable: false),
                    sobrecupo = table.Column<short>(type: "smallint", nullable: false),
                    sobrecupo_habilitado = table.Column<bool>(type: "boolean", nullable: false),
                    es_especial = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    notas = table.Column<string>(type: "text", nullable: true),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clases", x => x.id);
                    table.UniqueConstraint("AK_clases_box_id_id", x => new { x.box_id, x.id });
                    table.CheckConstraint("ck_clases_estado", "estado IN ('ACTIVA', 'CANCELADA')");
                    table.ForeignKey(
                        name: "FK_clases_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_clases_clase_programaciones_box_id_programacion_id",
                        columns: x => new { x.box_id, x.programacion_id },
                        principalTable: "clase_programaciones",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_clases_personas_box_id_coach_id",
                        columns: x => new { x.box_id, x.coach_id },
                        principalTable: "personas",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tipo_membresia_restricciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    tipo_membresia_id = table.Column<int>(type: "integer", nullable: false),
                    programacion_id = table.Column<int>(type: "integer", nullable: true),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    dias_semana = table.Column<short[]>(type: "smallint[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_membresia_restricciones", x => x.id);
                    table.CheckConstraint("ck_tipo_membresia_restricciones_dias_semana", "dias_semana <@ ARRAY[1, 2, 3, 4, 5, 6, 7]::smallint[]");
                    table.CheckConstraint("ck_tipo_membresia_restricciones_programacion_u_horario", "(programacion_id IS NOT NULL AND hora_inicio IS NULL AND hora_fin IS NULL AND dias_semana IS NULL) OR (programacion_id IS NULL AND hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND hora_fin > hora_inicio)");
                    table.ForeignKey(
                        name: "FK_tipo_membresia_restricciones_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tipo_membresia_restricciones_clase_programaciones_box_id_pr~",
                        columns: x => new { x.box_id, x.programacion_id },
                        principalTable: "clase_programaciones",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tipo_membresia_restricciones_tipos_membresia_box_id_tipo_me~",
                        columns: x => new { x.box_id, x.tipo_membresia_id },
                        principalTable: "tipos_membresia",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    membresia_id = table.Column<int>(type: "integer", nullable: false),
                    persona_id = table.Column<int>(type: "integer", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    metodo_pago_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_pago = table.Column<DateOnly>(type: "date", nullable: false),
                    registrado_por = table.Column<int>(type: "integer", nullable: false),
                    anulado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notas = table.Column<string>(type: "text", nullable: true),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos", x => x.id);
                    table.CheckConstraint("ck_pagos_monto", "monto > 0");
                    table.ForeignKey(
                        name: "FK_pagos_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_catalogos_box_id_metodo_pago_id",
                        columns: x => new { x.box_id, x.metodo_pago_id },
                        principalTable: "catalogos",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_membresias_box_id_membresia_id",
                        columns: x => new { x.box_id, x.membresia_id },
                        principalTable: "membresias",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_personas_box_id_persona_id",
                        columns: x => new { x.box_id, x.persona_id },
                        principalTable: "personas",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_usuarios_registrado_por",
                        column: x => x.registrado_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inscripciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    clase_id = table.Column<int>(type: "integer", nullable: false),
                    persona_id = table.Column<int>(type: "integer", nullable: false),
                    membresia_id = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    canal_reserva = table.Column<string>(type: "text", nullable: false),
                    canal_checkin = table.Column<string>(type: "text", nullable: true),
                    fecha_reserva = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_checkin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    checkin_by = table.Column<int>(type: "integer", nullable: true),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    editado_por = table.Column<int>(type: "integer", nullable: true),
                    fecha_edicion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inscripciones", x => x.id);
                    table.CheckConstraint("ck_inscripciones_canal_checkin", "canal_checkin IN ('COACH', 'PORTAL', 'QR')");
                    table.CheckConstraint("ck_inscripciones_canal_reserva", "canal_reserva IN ('COACH', 'PORTAL')");
                    table.CheckConstraint("ck_inscripciones_estado", "estado IN ('INSCRITO', 'PRESENTE', 'AUSENTE', 'CANCELADO')");
                    table.ForeignKey(
                        name: "FK_inscripciones_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inscripciones_clases_box_id_clase_id",
                        columns: x => new { x.box_id, x.clase_id },
                        principalTable: "clases",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inscripciones_membresias_box_id_membresia_id",
                        columns: x => new { x.box_id, x.membresia_id },
                        principalTable: "membresias",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inscripciones_personas_box_id_persona_id",
                        columns: x => new { x.box_id, x.persona_id },
                        principalTable: "personas",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inscripciones_usuarios_checkin_by",
                        column: x => x.checkin_by,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_boxes_public_id",
                table: "boxes",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_boxes_slug",
                table: "boxes",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalogos_box_id_tipo_nombre",
                table: "catalogos",
                columns: new[] { "box_id", "tipo", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalogos_public_id",
                table: "catalogos",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clase_programacion_dias_box_id_programacion_id",
                table: "clase_programacion_dias",
                columns: new[] { "box_id", "programacion_id" });

            migrationBuilder.CreateIndex(
                name: "IX_clase_programacion_dias_programacion_id_dia_semana",
                table: "clase_programacion_dias",
                columns: new[] { "programacion_id", "dia_semana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clase_programaciones_box_id_coach_id",
                table: "clase_programaciones",
                columns: new[] { "box_id", "coach_id" });

            migrationBuilder.CreateIndex(
                name: "IX_clase_programaciones_public_id",
                table: "clase_programaciones",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clases_box_id_coach_id",
                table: "clases",
                columns: new[] { "box_id", "coach_id" });

            migrationBuilder.CreateIndex(
                name: "IX_clases_box_id_fecha",
                table: "clases",
                columns: new[] { "box_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_clases_box_id_programacion_id",
                table: "clases",
                columns: new[] { "box_id", "programacion_id" });

            migrationBuilder.CreateIndex(
                name: "IX_clases_programacion_id_fecha",
                table: "clases",
                columns: new[] { "programacion_id", "fecha" },
                unique: true,
                filter: "programacion_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_clases_public_id",
                table: "clases",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contactos_emergencia_box_id_persona_id",
                table: "contactos_emergencia",
                columns: new[] { "box_id", "persona_id" });

            migrationBuilder.CreateIndex(
                name: "IX_contactos_emergencia_public_id",
                table: "contactos_emergencia",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_box_id_clase_id",
                table: "inscripciones",
                columns: new[] { "box_id", "clase_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_box_id_membresia_id",
                table: "inscripciones",
                columns: new[] { "box_id", "membresia_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_box_id_persona_id",
                table: "inscripciones",
                columns: new[] { "box_id", "persona_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_checkin_by",
                table: "inscripciones",
                column: "checkin_by");

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_clase_id_persona_id",
                table: "inscripciones",
                columns: new[] { "clase_id", "persona_id" },
                unique: true,
                filter: "estado <> 'CANCELADO'");

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_public_id",
                table: "inscripciones",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_membresias_box_id_fecha_fin",
                table: "membresias",
                columns: new[] { "box_id", "fecha_fin" });

            migrationBuilder.CreateIndex(
                name: "IX_membresias_box_id_persona_id",
                table: "membresias",
                columns: new[] { "box_id", "persona_id" });

            migrationBuilder.CreateIndex(
                name: "IX_membresias_box_id_persona_vinculada_id",
                table: "membresias",
                columns: new[] { "box_id", "persona_vinculada_id" });

            migrationBuilder.CreateIndex(
                name: "IX_membresias_box_id_tipo_membresia_id",
                table: "membresias",
                columns: new[] { "box_id", "tipo_membresia_id" });

            migrationBuilder.CreateIndex(
                name: "IX_membresias_cancelada_por",
                table: "membresias",
                column: "cancelada_por");

            migrationBuilder.CreateIndex(
                name: "IX_membresias_public_id",
                table: "membresias",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_box_id_fecha_pago",
                table: "pagos",
                columns: new[] { "box_id", "fecha_pago" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_box_id_membresia_id",
                table: "pagos",
                columns: new[] { "box_id", "membresia_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_box_id_metodo_pago_id",
                table: "pagos",
                columns: new[] { "box_id", "metodo_pago_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_box_id_persona_id",
                table: "pagos",
                columns: new[] { "box_id", "persona_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_public_id",
                table: "pagos",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_registrado_por",
                table: "pagos",
                column: "registrado_por");

            migrationBuilder.CreateIndex(
                name: "IX_personas_box_id_apellidos_nombre",
                table: "personas",
                columns: new[] { "box_id", "apellidos", "nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_personas_box_id_codigo",
                table: "personas",
                columns: new[] { "box_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_personas_public_id",
                table: "personas",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_box_id",
                table: "refresh_tokens",
                column: "box_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_usuario_id_box_id",
                table: "refresh_tokens",
                columns: new[] { "usuario_id", "box_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tipo_membresia_restricciones_box_id_programacion_id",
                table: "tipo_membresia_restricciones",
                columns: new[] { "box_id", "programacion_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tipo_membresia_restricciones_box_id_tipo_membresia_id",
                table: "tipo_membresia_restricciones",
                columns: new[] { "box_id", "tipo_membresia_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tipos_membresia_box_id_categoria_id",
                table: "tipos_membresia",
                columns: new[] { "box_id", "categoria_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tipos_membresia_box_id_nombre",
                table: "tipos_membresia",
                columns: new[] { "box_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipos_membresia_public_id",
                table: "tipos_membresia",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_boxes_box_id_persona_id",
                table: "usuario_boxes",
                columns: new[] { "box_id", "persona_id" });

            migrationBuilder.CreateIndex(
                name: "IX_usuario_boxes_usuario_id_box_id",
                table: "usuario_boxes",
                columns: new[] { "usuario_id", "box_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_public_id",
                table: "usuarios",
                column: "public_id",
                unique: true);

            // Índice de expresión: EF no lo modela, ver UsuarioConfiguration.
            migrationBuilder.Sql("""CREATE UNIQUE INDEX "UX_usuarios_email_lower" ON usuarios (lower(email));""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "UX_usuarios_email_lower";""");

            migrationBuilder.DropTable(
                name: "clase_programacion_dias");

            migrationBuilder.DropTable(
                name: "contactos_emergencia");

            migrationBuilder.DropTable(
                name: "inscripciones");

            migrationBuilder.DropTable(
                name: "pagos");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "tipo_membresia_restricciones");

            migrationBuilder.DropTable(
                name: "usuario_boxes");

            migrationBuilder.DropTable(
                name: "clases");

            migrationBuilder.DropTable(
                name: "membresias");

            migrationBuilder.DropTable(
                name: "clase_programaciones");

            migrationBuilder.DropTable(
                name: "tipos_membresia");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "personas");

            migrationBuilder.DropTable(
                name: "catalogos");

            migrationBuilder.DropTable(
                name: "boxes");
        }
    }
}
