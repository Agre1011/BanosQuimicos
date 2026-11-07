using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanosQuimicos.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class ClaveEnvioSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClaveEnvio",
                table: "SOLICITUDES_CLIENTE",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SOLICITUDES_CLIENTE_ClaveEnvio",
                table: "SOLICITUDES_CLIENTE",
                column: "ClaveEnvio",
                unique: true,
                filter: "[ClaveEnvio] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SOLICITUDES_CLIENTE_ClaveEnvio",
                table: "SOLICITUDES_CLIENTE");

            migrationBuilder.DropColumn(
                name: "ClaveEnvio",
                table: "SOLICITUDES_CLIENTE");
        }
    }
}
