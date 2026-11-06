using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanosQuimicos.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class ResponsableLimpiezaYProgramacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClaveProgramacion",
                table: "TAREAS_OPERATIVAS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdResponsableLimpieza",
                table: "BANOS_QUIMICOS",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_ClaveProgramacion",
                table: "TAREAS_OPERATIVAS",
                column: "ClaveProgramacion",
                unique: true,
                filter: "[ClaveProgramacion] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BANOS_QUIMICOS_IdResponsableLimpieza",
                table: "BANOS_QUIMICOS",
                column: "IdResponsableLimpieza");

            migrationBuilder.AddForeignKey(
                name: "FK_BANOS_QUIMICOS_USUARIOS_IdResponsableLimpieza",
                table: "BANOS_QUIMICOS",
                column: "IdResponsableLimpieza",
                principalTable: "USUARIOS",
                principalColumn: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BANOS_QUIMICOS_USUARIOS_IdResponsableLimpieza",
                table: "BANOS_QUIMICOS");

            migrationBuilder.DropIndex(
                name: "IX_TAREAS_OPERATIVAS_ClaveProgramacion",
                table: "TAREAS_OPERATIVAS");

            migrationBuilder.DropIndex(
                name: "IX_BANOS_QUIMICOS_IdResponsableLimpieza",
                table: "BANOS_QUIMICOS");

            migrationBuilder.DropColumn(
                name: "ClaveProgramacion",
                table: "TAREAS_OPERATIVAS");

            migrationBuilder.DropColumn(
                name: "IdResponsableLimpieza",
                table: "BANOS_QUIMICOS");
        }
    }
}
