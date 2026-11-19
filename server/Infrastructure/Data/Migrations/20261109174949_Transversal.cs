using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RxBox.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Transversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "membresia_anterior_id",
                table: "membresias",
                type: "integer",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_pagos_box_id_id",
                table: "pagos",
                columns: new[] { "box_id", "id" });

            migrationBuilder.CreateTable(
                name: "bitacora",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: true),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    entidad = table.Column<string>(type: "text", nullable: false),
                    entidad_public_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accion = table.Column<string>(type: "text", nullable: false),
                    cambios = table.Column<string>(type: "jsonb", nullable: true),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bitacora", x => x.id);
                    table.CheckConstraint("ck_bitacora_accion", "accion IN ('CREAR', 'EDITAR', 'ELIMINAR', 'LOGIN', 'LOGIN_FALLIDO', 'CAMBIO_BOX')");
                });

            migrationBuilder.CreateTable(
                name: "membresia_eventos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    box_id = table.Column<int>(type: "integer", nullable: false),
                    membresia_id = table.Column<int>(type: "integer", nullable: false),
                    persona_id = table.Column<int>(type: "integer", nullable: false),
                    pago_id = table.Column<int>(type: "integer", nullable: true),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    datos = table.Column<string>(type: "jsonb", nullable: true),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membresia_eventos", x => x.id);
                    table.CheckConstraint("ck_membresia_eventos_tipo", "tipo IN ('ALTA', 'RENOVACION', 'PAGO', 'PAGO_ANULADO', 'EXTENSION', 'CANCELACION')");
                    table.ForeignKey(
                        name: "FK_membresia_eventos_boxes_box_id",
                        column: x => x.box_id,
                        principalTable: "boxes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membresia_eventos_membresias_box_id_membresia_id",
                        columns: x => new { x.box_id, x.membresia_id },
                        principalTable: "membresias",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membresia_eventos_pagos_box_id_pago_id",
                        columns: x => new { x.box_id, x.pago_id },
                        principalTable: "pagos",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membresia_eventos_personas_box_id_persona_id",
                        columns: x => new { x.box_id, x.persona_id },
                        principalTable: "personas",
                        principalColumns: new[] { "box_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membresia_eventos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_membresias_membresia_anterior_id",
                table: "membresias",
                column: "membresia_anterior_id",
                unique: true,
                filter: "membresia_anterior_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_bitacora_box_id_entidad_entidad_public_id",
                table: "bitacora",
                columns: new[] { "box_id", "entidad", "entidad_public_id" });

            migrationBuilder.CreateIndex(
                name: "IX_bitacora_box_id_fecha",
                table: "bitacora",
                columns: new[] { "box_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_membresia_eventos_box_id_membresia_id_fecha",
                table: "membresia_eventos",
                columns: new[] { "box_id", "membresia_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_membresia_eventos_box_id_pago_id",
                table: "membresia_eventos",
                columns: new[] { "box_id", "pago_id" });

            migrationBuilder.CreateIndex(
                name: "IX_membresia_eventos_box_id_persona_id_fecha",
                table: "membresia_eventos",
                columns: new[] { "box_id", "persona_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_membresia_eventos_usuario_id",
                table: "membresia_eventos",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bitacora");

            migrationBuilder.DropTable(
                name: "membresia_eventos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_pagos_box_id_id",
                table: "pagos");

            migrationBuilder.DropIndex(
                name: "IX_membresias_membresia_anterior_id",
                table: "membresias");

            migrationBuilder.DropColumn(
                name: "membresia_anterior_id",
                table: "membresias");
        }
    }
}
