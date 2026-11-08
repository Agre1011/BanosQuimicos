using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanosQuimicos.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class InventarioBanosYMantenimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescripcionTrabajo",
                table: "TAREAS_OPERATIVAS",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClaveAlta",
                table: "BANOS_QUIMICOS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaBaja",
                table: "BANOS_QUIMICOS",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BANOS_QUIMICOS_ClaveAlta",
                table: "BANOS_QUIMICOS",
                column: "ClaveAlta",
                unique: true,
                filter: "[ClaveAlta] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BANOS_QUIMICOS_FechaBaja",
                table: "BANOS_QUIMICOS",
                sql: "[FechaBaja] IS NULL OR [FechaBaja] >= [FechaAlta]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BANOS_QUIMICOS_ClaveAlta",
                table: "BANOS_QUIMICOS");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BANOS_QUIMICOS_FechaBaja",
                table: "BANOS_QUIMICOS");

            migrationBuilder.DropColumn(
                name: "DescripcionTrabajo",
                table: "TAREAS_OPERATIVAS");

            migrationBuilder.DropColumn(
                name: "ClaveAlta",
                table: "BANOS_QUIMICOS");

            migrationBuilder.DropColumn(
                name: "FechaBaja",
                table: "BANOS_QUIMICOS");
        }
    }
}
