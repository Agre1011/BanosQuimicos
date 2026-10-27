using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BanosQuimicos.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ESTADOS_BANO",
                columns: table => new
                {
                    IdEstadoBano = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreEstado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESTADOS_BANO", x => x.IdEstadoBano);
                });

            migrationBuilder.CreateTable(
                name: "ESTADOS_OPERACION",
                columns: table => new
                {
                    IdEstadoOperacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreEstado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ambito = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESTADOS_OPERACION", x => x.IdEstadoOperacion);
                    table.CheckConstraint("CK_ESTADOS_OPERACION_Ambito", "[Ambito] IN (N'Solicitud', N'Tarea')");
                });

            migrationBuilder.CreateTable(
                name: "ROLES",
                columns: table => new
                {
                    IdRol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreRol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLES", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "TIPOS_BANO",
                columns: table => new
                {
                    IdTipoBano = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreTipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIPOS_BANO", x => x.IdTipoBano);
                });

            migrationBuilder.CreateTable(
                name: "TIPOS_OPERACION",
                columns: table => new
                {
                    IdTipoOperacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreOperacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AplicaASolicitudes = table.Column<bool>(type: "bit", nullable: false),
                    AplicaATareas = table.Column<bool>(type: "bit", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIPOS_OPERACION", x => x.IdTipoOperacion);
                    table.CheckConstraint("CK_TIPOS_OPERACION_Aplicacion", "[AplicaASolicitudes] = 1 OR [AplicaATareas] = 1");
                });

            migrationBuilder.CreateTable(
                name: "USUARIOS",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    Telefono = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ContrasenaHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IdRol = table.Column<int>(type: "int", nullable: false),
                    EstadoCuenta = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MotivoSuspension = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaBaja = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIOS", x => x.IdUsuario);
                    table.CheckConstraint("CK_USUARIOS_DatosObligatorios", "LEN(LTRIM(RTRIM([Nombre]))) > 0 AND LEN(LTRIM(RTRIM([Apellido]))) > 0 AND LEN(LTRIM(RTRIM([Telefono]))) > 0 AND LEN([ContrasenaHash]) > 0");
                    table.CheckConstraint("CK_USUARIOS_Email", "DATALENGTH([Email]) = DATALENGTH(LTRIM(RTRIM([Email]))) AND LEN([Email]) > 0");
                    table.CheckConstraint("CK_USUARIOS_EstadoCuenta", "[EstadoCuenta] IN (N'Pendiente', N'Activa', N'Suspendida', N'BajaLogica')");
                    table.ForeignKey(
                        name: "FK_USUARIOS_ROLES_IdRol",
                        column: x => x.IdRol,
                        principalTable: "ROLES",
                        principalColumn: "IdRol");
                });

            migrationBuilder.CreateTable(
                name: "BANOS_QUIMICOS",
                columns: table => new
                {
                    IdBano = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoInterno = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    IdTipoBano = table.Column<int>(type: "int", nullable: false),
                    IdEstadoBano = table.Column<int>(type: "int", nullable: false),
                    FechaAlta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BANOS_QUIMICOS", x => x.IdBano);
                    table.CheckConstraint("CK_BANOS_QUIMICOS_Codigo", "DATALENGTH([CodigoInterno]) = DATALENGTH(LTRIM(RTRIM([CodigoInterno]))) AND LEN([CodigoInterno]) > 0");
                    table.ForeignKey(
                        name: "FK_BANOS_QUIMICOS_ESTADOS_BANO_IdEstadoBano",
                        column: x => x.IdEstadoBano,
                        principalTable: "ESTADOS_BANO",
                        principalColumn: "IdEstadoBano");
                    table.ForeignKey(
                        name: "FK_BANOS_QUIMICOS_TIPOS_BANO_IdTipoBano",
                        column: x => x.IdTipoBano,
                        principalTable: "TIPOS_BANO",
                        principalColumn: "IdTipoBano");
                });

            migrationBuilder.CreateTable(
                name: "CLIENTES",
                columns: table => new
                {
                    IdCliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Cuit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DireccionPrincipal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLIENTES", x => x.IdCliente);
                    table.ForeignKey(
                        name: "FK_CLIENTES_USUARIOS_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "USUARIOS",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateTable(
                name: "SERVICIOS",
                columns: table => new
                {
                    IdServicio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    IdBano = table.Column<int>(type: "int", nullable: false),
                    IdSolicitudOrigen = table.Column<int>(type: "int", nullable: true),
                    TipoUso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CantidadLimpiezasSemana = table.Column<int>(type: "int", nullable: false),
                    EstadoServicio = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaAsignacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaEntregaProgramada = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaEntregaReal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaRetiroReal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaFinalizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaCancelacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SERVICIOS", x => x.IdServicio);
                    table.CheckConstraint("CK_SERVICIOS_Ciclo", "([EstadoServicio] = N'Pendiente de entrega' AND [FechaEntregaReal] IS NULL AND [FechaRetiroReal] IS NULL AND [FechaFinalizacion] IS NULL AND [FechaCancelacion] IS NULL) OR ([EstadoServicio] = N'Entregado' AND [FechaEntregaReal] IS NOT NULL AND [FechaRetiroReal] IS NULL AND [FechaFinalizacion] IS NULL AND [FechaCancelacion] IS NULL) OR ([EstadoServicio] = N'Finalizado' AND [FechaEntregaReal] IS NOT NULL AND [FechaRetiroReal] IS NOT NULL AND [FechaFinalizacion] IS NOT NULL AND [FechaCancelacion] IS NULL) OR ([EstadoServicio] = N'Cancelado' AND [FechaEntregaReal] IS NULL AND [FechaRetiroReal] IS NULL AND [FechaFinalizacion] IS NULL AND [FechaCancelacion] IS NOT NULL)");
                    table.CheckConstraint("CK_SERVICIOS_Fechas", "([FechaEntregaReal] IS NULL OR [FechaEntregaReal] >= [FechaAsignacion]) AND ([FechaRetiroReal] IS NULL OR [FechaRetiroReal] >= [FechaEntregaReal]) AND ([FechaFinalizacion] IS NULL OR [FechaFinalizacion] >= [FechaRetiroReal]) AND ([FechaCancelacion] IS NULL OR [FechaCancelacion] >= [FechaAsignacion])");
                    table.CheckConstraint("CK_SERVICIOS_Frecuencia", "[CantidadLimpiezasSemana] >= 0");
                    table.CheckConstraint("CK_SERVICIOS_TipoUso", "[TipoUso] IN (N'Obra', N'Evento')");
                    table.ForeignKey(
                        name: "FK_SERVICIOS_BANOS_QUIMICOS_IdBano",
                        column: x => x.IdBano,
                        principalTable: "BANOS_QUIMICOS",
                        principalColumn: "IdBano");
                    table.ForeignKey(
                        name: "FK_SERVICIOS_CLIENTES_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "CLIENTES",
                        principalColumn: "IdCliente");
                });

            migrationBuilder.CreateTable(
                name: "SOLICITUDES_CLIENTE",
                columns: table => new
                {
                    IdSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    IdTipoOperacion = table.Column<int>(type: "int", nullable: false),
                    IdEstadoOperacion = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IdServicioReferencia = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CantidadBanos = table.Column<int>(type: "int", nullable: true),
                    IdTipoBanoSolicitado = table.Column<int>(type: "int", nullable: true),
                    TipoUsoSolicitado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DireccionSolicitada = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FechaDeseada = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NuevaDireccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ModalidadCambioDireccion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaCambioEfectiva = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaValidacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaResolucion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ObservacionesAdministrador = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SOLICITUDES_CLIENTE", x => x.IdSolicitud);
                    table.CheckConstraint("CK_SOLICITUDES_CLIENTE_Cantidad", "[CantidadBanos] IS NULL OR [CantidadBanos] > 0");
                    table.CheckConstraint("CK_SOLICITUDES_CLIENTE_Modalidad", "[ModalidadCambioDireccion] IS NULL OR [ModalidadCambioDireccion] IN (N'Solicitado', N'Informado')");
                    table.CheckConstraint("CK_SOLICITUDES_CLIENTE_Uso", "[TipoUsoSolicitado] IS NULL OR [TipoUsoSolicitado] IN (N'Obra', N'Evento')");
                    table.ForeignKey(
                        name: "FK_SOLICITUDES_CLIENTE_CLIENTES_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "CLIENTES",
                        principalColumn: "IdCliente");
                    table.ForeignKey(
                        name: "FK_SOLICITUDES_CLIENTE_ESTADOS_OPERACION_IdEstadoOperacion",
                        column: x => x.IdEstadoOperacion,
                        principalTable: "ESTADOS_OPERACION",
                        principalColumn: "IdEstadoOperacion");
                    table.ForeignKey(
                        name: "FK_SOLICITUDES_CLIENTE_SERVICIOS_IdServicioReferencia",
                        column: x => x.IdServicioReferencia,
                        principalTable: "SERVICIOS",
                        principalColumn: "IdServicio");
                    table.ForeignKey(
                        name: "FK_SOLICITUDES_CLIENTE_TIPOS_BANO_IdTipoBanoSolicitado",
                        column: x => x.IdTipoBanoSolicitado,
                        principalTable: "TIPOS_BANO",
                        principalColumn: "IdTipoBano");
                    table.ForeignKey(
                        name: "FK_SOLICITUDES_CLIENTE_TIPOS_OPERACION_IdTipoOperacion",
                        column: x => x.IdTipoOperacion,
                        principalTable: "TIPOS_OPERACION",
                        principalColumn: "IdTipoOperacion");
                });

            migrationBuilder.CreateTable(
                name: "UBICACIONES_SERVICIO",
                columns: table => new
                {
                    IdUbicacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdServicio = table.Column<int>(type: "int", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Localidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Referencia = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EsActual = table.Column<bool>(type: "bit", nullable: false),
                    FechaDesde = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaHasta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaValidacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UBICACIONES_SERVICIO", x => x.IdUbicacion);
                    table.CheckConstraint("CK_UBICACIONES_SERVICIO_Direccion", "LEN(LTRIM(RTRIM([Direccion]))) > 0");
                    table.CheckConstraint("CK_UBICACIONES_SERVICIO_Intervalo", "([FechaHasta] IS NULL OR ([FechaDesde] IS NOT NULL AND [FechaHasta] >= [FechaDesde])) AND ([EsActual] = 0 OR ([FechaDesde] IS NOT NULL AND [FechaHasta] IS NULL))");
                    table.ForeignKey(
                        name: "FK_UBICACIONES_SERVICIO_SERVICIOS_IdServicio",
                        column: x => x.IdServicio,
                        principalTable: "SERVICIOS",
                        principalColumn: "IdServicio");
                });

            migrationBuilder.CreateTable(
                name: "TAREAS_OPERATIVAS",
                columns: table => new
                {
                    IdTarea = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdBano = table.Column<int>(type: "int", nullable: false),
                    IdTipoOperacion = table.Column<int>(type: "int", nullable: false),
                    IdEstadoOperacion = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BloqueaAsignacion = table.Column<bool>(type: "bit", nullable: false),
                    IdServicio = table.Column<int>(type: "int", nullable: true),
                    IdEmpleado = table.Column<int>(type: "int", nullable: true),
                    IdSolicitud = table.Column<int>(type: "int", nullable: true),
                    IdUbicacion = table.Column<int>(type: "int", nullable: true),
                    IdUbicacionDestino = table.Column<int>(type: "int", nullable: true),
                    FechaProgramada = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaInicioReal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaRealizada = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaCancelacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PapelHigienicoRepuesto = table.Column<bool>(type: "bit", nullable: true),
                    ObservacionesEmpleado = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAREAS_OPERATIVAS", x => x.IdTarea);
                    table.CheckConstraint("CK_TAREAS_OPERATIVAS_Bloqueo", "[BloqueaAsignacion] = 0 OR [IdTipoOperacion] = 11");
                    table.CheckConstraint("CK_TAREAS_OPERATIVAS_Empleado", "[IdEstadoOperacion] NOT IN (7, 8) OR [IdEmpleado] IS NOT NULL");
                    table.CheckConstraint("CK_TAREAS_OPERATIVAS_Servicio", "[IdServicio] IS NOT NULL OR [IdTipoOperacion] = 11");
                    table.ForeignKey(
                        name: "FK_TAREAS_OPERATIVAS_BANOS_QUIMICOS_IdBano",
                        column: x => x.IdBano,
                        principalTable: "BANOS_QUIMICOS",
                        principalColumn: "IdBano");
                    table.ForeignKey(
                        name: "FK_TAREAS_OPERATIVAS_ESTADOS_OPERACION_IdEstadoOperacion",
                        column: x => x.IdEstadoOperacion,
                        principalTable: "ESTADOS_OPERACION",
                        principalColumn: "IdEstadoOperacion");
                    table.ForeignKey(
                        name: "FK_TAREAS_OPERATIVAS_SERVICIOS_IdServicio",
                        column: x => x.IdServicio,
                        principalTable: "SERVICIOS",
                        principalColumn: "IdServicio");
                    table.ForeignKey(
                        name: "FK_TAREAS_OPERATIVAS_SOLICITUDES_CLIENTE_IdSolicitud",
                        column: x => x.IdSolicitud,
                        principalTable: "SOLICITUDES_CLIENTE",
                        principalColumn: "IdSolicitud");
                    table.ForeignKey(
                        name: "FK_TAREAS_OPERATIVAS_TIPOS_OPERACION_IdTipoOperacion",
                        column: x => x.IdTipoOperacion,
                        principalTable: "TIPOS_OPERACION",
                        principalColumn: "IdTipoOperacion");
                    table.ForeignKey(
                        name: "FK_TAREAS_OPERATIVAS_UBICACIONES_SERVICIO_IdUbicacion",
                        column: x => x.IdUbicacion,
                        principalTable: "UBICACIONES_SERVICIO",
                        principalColumn: "IdUbicacion");
                    table.ForeignKey(
                        name: "FK_TAREAS_OPERATIVAS_UBICACIONES_SERVICIO_IdUbicacionDestino",
                        column: x => x.IdUbicacionDestino,
                        principalTable: "UBICACIONES_SERVICIO",
                        principalColumn: "IdUbicacion");
                    table.ForeignKey(
                        name: "FK_TAREAS_OPERATIVAS_USUARIOS_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "USUARIOS",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.InsertData(
                table: "ESTADOS_BANO",
                columns: new[] { "IdEstadoBano", "Descripcion", "NombreEstado" },
                values: new object[,]
                {
                    { 1, null, "Apto" },
                    { 2, null, "No apto" }
                });

            migrationBuilder.InsertData(
                table: "ESTADOS_OPERACION",
                columns: new[] { "IdEstadoOperacion", "Ambito", "Descripcion", "NombreEstado" },
                values: new object[,]
                {
                    { 1, "Solicitud", null, "Pendiente" },
                    { 2, "Solicitud", null, "Aprobada" },
                    { 3, "Solicitud", null, "Rechazada" },
                    { 4, "Solicitud", null, "Resuelta" },
                    { 5, "Solicitud", null, "Cancelada" },
                    { 6, "Tarea", null, "Pendiente" },
                    { 7, "Tarea", null, "En curso" },
                    { 8, "Tarea", null, "Realizada" },
                    { 9, "Tarea", null, "Cancelada" }
                });

            migrationBuilder.InsertData(
                table: "ROLES",
                columns: new[] { "IdRol", "NombreRol" },
                values: new object[,]
                {
                    { 1, "Administrador" },
                    { 2, "Empleado" },
                    { 3, "Cliente" }
                });

            migrationBuilder.InsertData(
                table: "TIPOS_BANO",
                columns: new[] { "IdTipoBano", "Descripcion", "NombreTipo" },
                values: new object[,]
                {
                    { 1, null, "Obra" },
                    { 2, null, "Evento" }
                });

            migrationBuilder.InsertData(
                table: "TIPOS_OPERACION",
                columns: new[] { "IdTipoOperacion", "AplicaASolicitudes", "AplicaATareas", "Descripcion", "NombreOperacion" },
                values: new object[,]
                {
                    { 1, true, false, null, "Nuevo servicio" },
                    { 2, true, false, null, "Baño adicional" },
                    { 3, true, true, null, "Limpieza extra" },
                    { 4, true, false, null, "Cambio de dirección" },
                    { 5, true, false, null, "Reclamo" },
                    { 6, true, false, null, "Baja" },
                    { 7, false, true, null, "Entrega" },
                    { 8, false, true, null, "Retiro" },
                    { 9, false, true, null, "Limpieza semanal" },
                    { 10, false, true, null, "Traslado" },
                    { 11, false, true, null, "Mantenimiento" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BANOS_QUIMICOS_CodigoInterno",
                table: "BANOS_QUIMICOS",
                column: "CodigoInterno",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BANOS_QUIMICOS_IdEstadoBano",
                table: "BANOS_QUIMICOS",
                column: "IdEstadoBano");

            migrationBuilder.CreateIndex(
                name: "IX_BANOS_QUIMICOS_IdTipoBano",
                table: "BANOS_QUIMICOS",
                column: "IdTipoBano");

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTES_IdUsuario",
                table: "CLIENTES",
                column: "IdUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ESTADOS_BANO_NombreEstado",
                table: "ESTADOS_BANO",
                column: "NombreEstado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ESTADOS_OPERACION_Ambito_NombreEstado",
                table: "ESTADOS_OPERACION",
                columns: new[] { "Ambito", "NombreEstado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROLES_NombreRol",
                table: "ROLES",
                column: "NombreRol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SERVICIOS_IdCliente",
                table: "SERVICIOS",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICIOS_IdSolicitudOrigen",
                table: "SERVICIOS",
                column: "IdSolicitudOrigen");

            migrationBuilder.CreateIndex(
                name: "UX_SERVICIOS_BanoAsignado",
                table: "SERVICIOS",
                column: "IdBano",
                unique: true,
                filter: "[EstadoServicio] IN (N'Pendiente de entrega', N'Entregado')");

            migrationBuilder.CreateIndex(
                name: "IX_SOLICITUDES_CLIENTE_IdCliente",
                table: "SOLICITUDES_CLIENTE",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_SOLICITUDES_CLIENTE_IdEstadoOperacion",
                table: "SOLICITUDES_CLIENTE",
                column: "IdEstadoOperacion");

            migrationBuilder.CreateIndex(
                name: "IX_SOLICITUDES_CLIENTE_IdServicioReferencia",
                table: "SOLICITUDES_CLIENTE",
                column: "IdServicioReferencia");

            migrationBuilder.CreateIndex(
                name: "IX_SOLICITUDES_CLIENTE_IdTipoBanoSolicitado",
                table: "SOLICITUDES_CLIENTE",
                column: "IdTipoBanoSolicitado");

            migrationBuilder.CreateIndex(
                name: "IX_SOLICITUDES_CLIENTE_IdTipoOperacion",
                table: "SOLICITUDES_CLIENTE",
                column: "IdTipoOperacion");

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_IdBano_BloqueaAsignacion_IdEstadoOperacion",
                table: "TAREAS_OPERATIVAS",
                columns: new[] { "IdBano", "BloqueaAsignacion", "IdEstadoOperacion" });

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_IdEmpleado",
                table: "TAREAS_OPERATIVAS",
                column: "IdEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_IdEstadoOperacion",
                table: "TAREAS_OPERATIVAS",
                column: "IdEstadoOperacion");

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_IdServicio",
                table: "TAREAS_OPERATIVAS",
                column: "IdServicio");

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_IdSolicitud",
                table: "TAREAS_OPERATIVAS",
                column: "IdSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_IdTipoOperacion",
                table: "TAREAS_OPERATIVAS",
                column: "IdTipoOperacion");

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_IdUbicacion",
                table: "TAREAS_OPERATIVAS",
                column: "IdUbicacion");

            migrationBuilder.CreateIndex(
                name: "IX_TAREAS_OPERATIVAS_IdUbicacionDestino",
                table: "TAREAS_OPERATIVAS",
                column: "IdUbicacionDestino");

            migrationBuilder.CreateIndex(
                name: "IX_TIPOS_BANO_NombreTipo",
                table: "TIPOS_BANO",
                column: "NombreTipo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TIPOS_OPERACION_NombreOperacion",
                table: "TIPOS_OPERACION",
                column: "NombreOperacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UBICACIONES_SERVICIO_Actual",
                table: "UBICACIONES_SERVICIO",
                column: "IdServicio",
                unique: true,
                filter: "[EsActual] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_USUARIOS_Email",
                table: "USUARIOS",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USUARIOS_IdRol",
                table: "USUARIOS",
                column: "IdRol");

            migrationBuilder.AddForeignKey(
                name: "FK_SERVICIOS_SOLICITUDES_CLIENTE_IdSolicitudOrigen",
                table: "SERVICIOS",
                column: "IdSolicitudOrigen",
                principalTable: "SOLICITUDES_CLIENTE",
                principalColumn: "IdSolicitud");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BANOS_QUIMICOS_ESTADOS_BANO_IdEstadoBano",
                table: "BANOS_QUIMICOS");

            migrationBuilder.DropForeignKey(
                name: "FK_BANOS_QUIMICOS_TIPOS_BANO_IdTipoBano",
                table: "BANOS_QUIMICOS");

            migrationBuilder.DropForeignKey(
                name: "FK_SOLICITUDES_CLIENTE_TIPOS_BANO_IdTipoBanoSolicitado",
                table: "SOLICITUDES_CLIENTE");

            migrationBuilder.DropForeignKey(
                name: "FK_CLIENTES_USUARIOS_IdUsuario",
                table: "CLIENTES");

            migrationBuilder.DropForeignKey(
                name: "FK_SERVICIOS_BANOS_QUIMICOS_IdBano",
                table: "SERVICIOS");

            migrationBuilder.DropForeignKey(
                name: "FK_SERVICIOS_CLIENTES_IdCliente",
                table: "SERVICIOS");

            migrationBuilder.DropForeignKey(
                name: "FK_SOLICITUDES_CLIENTE_CLIENTES_IdCliente",
                table: "SOLICITUDES_CLIENTE");

            migrationBuilder.DropForeignKey(
                name: "FK_SERVICIOS_SOLICITUDES_CLIENTE_IdSolicitudOrigen",
                table: "SERVICIOS");

            migrationBuilder.DropTable(
                name: "TAREAS_OPERATIVAS");

            migrationBuilder.DropTable(
                name: "UBICACIONES_SERVICIO");

            migrationBuilder.DropTable(
                name: "ESTADOS_BANO");

            migrationBuilder.DropTable(
                name: "TIPOS_BANO");

            migrationBuilder.DropTable(
                name: "USUARIOS");

            migrationBuilder.DropTable(
                name: "ROLES");

            migrationBuilder.DropTable(
                name: "BANOS_QUIMICOS");

            migrationBuilder.DropTable(
                name: "CLIENTES");

            migrationBuilder.DropTable(
                name: "SOLICITUDES_CLIENTE");

            migrationBuilder.DropTable(
                name: "ESTADOS_OPERACION");

            migrationBuilder.DropTable(
                name: "SERVICIOS");

            migrationBuilder.DropTable(
                name: "TIPOS_OPERACION");
        }
    }
}
