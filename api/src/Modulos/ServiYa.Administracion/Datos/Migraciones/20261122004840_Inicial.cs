using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiYa.Administracion.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "registro_bitacora",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entidad_tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entidad_id = table.Column<Guid>(type: "uuid", nullable: true),
                    detalle = table.Column<string>(type: "jsonb", nullable: false),
                    ip = table.Column<IPAddress>(type: "inet", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registro_bitacora", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registro_bitacora_actor_id",
                schema: "admin",
                table: "registro_bitacora",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_registro_bitacora_created_at",
                schema: "admin",
                table: "registro_bitacora",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_registro_bitacora_entidad_tipo_entidad_id",
                schema: "admin",
                table: "registro_bitacora",
                columns: new[] { "entidad_tipo", "entidad_id" });

            // Bitácora de solo inserción (FR-016, FR-042): los privilegios por defecto de init.sql le
            // dan a la app UPDATE y DELETE en toda tabla nueva; aquí se le quitan, junto con TRUNCATE.
            migrationBuilder.Sql("REVOKE UPDATE, DELETE, TRUNCATE ON admin.registro_bitacora FROM serviya_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registro_bitacora",
                schema: "admin");
        }
    }
}
