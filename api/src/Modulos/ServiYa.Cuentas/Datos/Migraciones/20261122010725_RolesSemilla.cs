using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServiYa.Cuentas.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class RolesSemilla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "cuentas",
                table: "roles",
                columns: new[] { "id", "updated_at", "concurrency_stamp", "created_at", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("0193e0a0-0000-7000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "0193e0a0-0000-7000-8000-000000000001", new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cliente", "CLIENTE" },
                    { new Guid("0193e0a0-0000-7000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "0193e0a0-0000-7000-8000-000000000002", new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Prestador", "PRESTADOR" },
                    { new Guid("0193e0a0-0000-7000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "0193e0a0-0000-7000-8000-000000000003", new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "cuentas",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("0193e0a0-0000-7000-8000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "cuentas",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("0193e0a0-0000-7000-8000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "cuentas",
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("0193e0a0-0000-7000-8000-000000000003"));
        }
    }
}
