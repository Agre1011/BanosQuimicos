using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanosQuimicos.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class RevisionUbicacionSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevisionUbicacionJson",
                table: "SOLICITUDES_CLIENTE",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevisionUbicacionJson",
                table: "SOLICITUDES_CLIENTE");
        }
    }
}
