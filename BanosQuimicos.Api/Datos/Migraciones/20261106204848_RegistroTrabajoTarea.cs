using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanosQuimicos.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class RegistroTrabajoTarea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaTrabajoRegistrado",
                table: "TAREAS_OPERATIVAS",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdEmpleadoRegistroTrabajo",
                table: "TAREAS_OPERATIVAS",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_IdEmpleadoRegistroTrabajo",
                table: "TAREAS_OPERATIVAS",
                column: "IdEmpleadoRegistroTrabajo");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TAREAS_OPERATIVAS_RegistroTrabajo",
                table: "TAREAS_OPERATIVAS",
                sql: "([FechaTrabajoRegistrado] IS NULL AND [IdEmpleadoRegistroTrabajo] IS NULL) OR ([FechaTrabajoRegistrado] IS NOT NULL AND [IdEmpleadoRegistroTrabajo] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_TAREAS_OPERATIVAS_USUARIOS_IdEmpleadoRegistroTrabajo",
                table: "TAREAS_OPERATIVAS",
                column: "IdEmpleadoRegistroTrabajo",
                principalTable: "USUARIOS",
                principalColumn: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TAREAS_OPERATIVAS_USUARIOS_IdEmpleadoRegistroTrabajo",
                table: "TAREAS_OPERATIVAS");

            migrationBuilder.DropIndex(
                name: "IX_TAREAS_OPERATIVAS_IdEmpleadoRegistroTrabajo",
                table: "TAREAS_OPERATIVAS");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TAREAS_OPERATIVAS_RegistroTrabajo",
                table: "TAREAS_OPERATIVAS");

            migrationBuilder.DropColumn(
                name: "FechaTrabajoRegistrado",
                table: "TAREAS_OPERATIVAS");

            migrationBuilder.DropColumn(
                name: "IdEmpleadoRegistroTrabajo",
                table: "TAREAS_OPERATIVAS");
        }
    }
}
