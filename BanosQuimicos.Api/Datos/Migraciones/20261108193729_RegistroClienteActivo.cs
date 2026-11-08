using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanosQuimicos.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class RegistroClienteActivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EstadoCuenta",
                table: "USUARIOS",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Activa",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pendiente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EstadoCuenta",
                table: "USUARIOS",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pendiente",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Activa");
        }
    }
}
