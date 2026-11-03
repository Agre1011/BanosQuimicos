-- Destino previsto: base existente y vacía db77593 de MonsterASP.
-- Generado por EF Core 10.0.12: desde 0 hasta 20261027203511_Inicial.
-- Seleccionar db77593 en el cliente SQL ANTES de ejecutar; el script no cambia de base.
-- Contiene esquema + catálogos, sin usuarios ni datos de LocalDB.
-- Revisión estática realizada; NO ejecutado. No es un script idempotente.
-- La tabla técnica de historial se crea antes de la transacción de la migración.

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [ESTADOS_BANO] (
    [IdEstadoBano] int NOT NULL IDENTITY,
    [NombreEstado] nvarchar(30) NOT NULL,
    [Descripcion] nvarchar(500) NULL,
    CONSTRAINT [PK_ESTADOS_BANO] PRIMARY KEY ([IdEstadoBano])
);

CREATE TABLE [ESTADOS_OPERACION] (
    [IdEstadoOperacion] int NOT NULL IDENTITY,
    [NombreEstado] nvarchar(30) NOT NULL,
    [Ambito] nvarchar(10) NOT NULL,
    [Descripcion] nvarchar(500) NULL,
    CONSTRAINT [PK_ESTADOS_OPERACION] PRIMARY KEY ([IdEstadoOperacion]),
    CONSTRAINT [CK_ESTADOS_OPERACION_Ambito] CHECK ([Ambito] IN (N'Solicitud', N'Tarea'))
);

CREATE TABLE [ROLES] (
    [IdRol] int NOT NULL IDENTITY,
    [NombreRol] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_ROLES] PRIMARY KEY ([IdRol])
);

CREATE TABLE [TIPOS_BANO] (
    [IdTipoBano] int NOT NULL IDENTITY,
    [NombreTipo] nvarchar(30) NOT NULL,
    [Descripcion] nvarchar(500) NULL,
    CONSTRAINT [PK_TIPOS_BANO] PRIMARY KEY ([IdTipoBano])
);

CREATE TABLE [TIPOS_OPERACION] (
    [IdTipoOperacion] int NOT NULL IDENTITY,
    [NombreOperacion] nvarchar(50) NOT NULL,
    [AplicaASolicitudes] bit NOT NULL,
    [AplicaATareas] bit NOT NULL,
    [Descripcion] nvarchar(500) NULL,
    CONSTRAINT [PK_TIPOS_OPERACION] PRIMARY KEY ([IdTipoOperacion]),
    CONSTRAINT [CK_TIPOS_OPERACION_Aplicacion] CHECK ([AplicaASolicitudes] = 1 OR [AplicaATareas] = 1)
);

CREATE TABLE [USUARIOS] (
    [IdUsuario] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [Apellido] nvarchar(100) NOT NULL,
    [Email] nvarchar(254) COLLATE Latin1_General_100_CI_AS NOT NULL,
    [Telefono] nvarchar(40) NOT NULL,
    [ContrasenaHash] nvarchar(1000) NOT NULL,
    [IdRol] int NOT NULL,
    [EstadoCuenta] nvarchar(20) NOT NULL DEFAULT N'Pendiente',
    [FechaRegistro] datetimeoffset NOT NULL,
    [MotivoSuspension] nvarchar(1000) NULL,
    [FechaBaja] datetimeoffset NULL,
    CONSTRAINT [PK_USUARIOS] PRIMARY KEY ([IdUsuario]),
    CONSTRAINT [CK_USUARIOS_DatosObligatorios] CHECK (LEN(LTRIM(RTRIM([Nombre]))) > 0 AND LEN(LTRIM(RTRIM([Apellido]))) > 0 AND LEN(LTRIM(RTRIM([Telefono]))) > 0 AND LEN([ContrasenaHash]) > 0),
    CONSTRAINT [CK_USUARIOS_Email] CHECK (DATALENGTH([Email]) = DATALENGTH(LTRIM(RTRIM([Email]))) AND LEN([Email]) > 0),
    CONSTRAINT [CK_USUARIOS_EstadoCuenta] CHECK ([EstadoCuenta] IN (N'Pendiente', N'Activa', N'Suspendida', N'BajaLogica')),
    CONSTRAINT [FK_USUARIOS_ROLES_IdRol] FOREIGN KEY ([IdRol]) REFERENCES [ROLES] ([IdRol])
);

CREATE TABLE [BANOS_QUIMICOS] (
    [IdBano] int NOT NULL IDENTITY,
    [CodigoInterno] nvarchar(30) COLLATE Latin1_General_100_CI_AS NOT NULL,
    [IdTipoBano] int NOT NULL,
    [IdEstadoBano] int NOT NULL,
    [FechaAlta] datetimeoffset NOT NULL,
    [Observaciones] nvarchar(2000) NULL,
    CONSTRAINT [PK_BANOS_QUIMICOS] PRIMARY KEY ([IdBano]),
    CONSTRAINT [CK_BANOS_QUIMICOS_Codigo] CHECK (DATALENGTH([CodigoInterno]) = DATALENGTH(LTRIM(RTRIM([CodigoInterno]))) AND LEN([CodigoInterno]) > 0),
    CONSTRAINT [FK_BANOS_QUIMICOS_ESTADOS_BANO_IdEstadoBano] FOREIGN KEY ([IdEstadoBano]) REFERENCES [ESTADOS_BANO] ([IdEstadoBano]),
    CONSTRAINT [FK_BANOS_QUIMICOS_TIPOS_BANO_IdTipoBano] FOREIGN KEY ([IdTipoBano]) REFERENCES [TIPOS_BANO] ([IdTipoBano])
);

CREATE TABLE [CLIENTES] (
    [IdCliente] int NOT NULL IDENTITY,
    [IdUsuario] int NOT NULL,
    [RazonSocial] nvarchar(200) NULL,
    [Cuit] nvarchar(20) NULL,
    [DireccionPrincipal] nvarchar(300) NULL,
    [Observaciones] nvarchar(2000) NULL,
    CONSTRAINT [PK_CLIENTES] PRIMARY KEY ([IdCliente]),
    CONSTRAINT [FK_CLIENTES_USUARIOS_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [USUARIOS] ([IdUsuario])
);

CREATE TABLE [SERVICIOS] (
    [IdServicio] int NOT NULL IDENTITY,
    [IdCliente] int NOT NULL,
    [IdBano] int NOT NULL,
    [IdSolicitudOrigen] int NULL,
    [TipoUso] nvarchar(10) NOT NULL,
    [CantidadLimpiezasSemana] int NOT NULL,
    [EstadoServicio] nvarchar(30) NOT NULL,
    [FechaAsignacion] datetimeoffset NOT NULL,
    [FechaEntregaProgramada] datetimeoffset NULL,
    [FechaEntregaReal] datetimeoffset NULL,
    [FechaRetiroReal] datetimeoffset NULL,
    [FechaFinalizacion] datetimeoffset NULL,
    [FechaCancelacion] datetimeoffset NULL,
    [Observaciones] nvarchar(2000) NULL,
    CONSTRAINT [PK_SERVICIOS] PRIMARY KEY ([IdServicio]),
    CONSTRAINT [CK_SERVICIOS_Ciclo] CHECK (([EstadoServicio] = N'Pendiente de entrega' AND [FechaEntregaReal] IS NULL AND [FechaRetiroReal] IS NULL AND [FechaFinalizacion] IS NULL AND [FechaCancelacion] IS NULL) OR ([EstadoServicio] = N'Entregado' AND [FechaEntregaReal] IS NOT NULL AND [FechaRetiroReal] IS NULL AND [FechaFinalizacion] IS NULL AND [FechaCancelacion] IS NULL) OR ([EstadoServicio] = N'Finalizado' AND [FechaEntregaReal] IS NOT NULL AND [FechaRetiroReal] IS NOT NULL AND [FechaFinalizacion] IS NOT NULL AND [FechaCancelacion] IS NULL) OR ([EstadoServicio] = N'Cancelado' AND [FechaEntregaReal] IS NULL AND [FechaRetiroReal] IS NULL AND [FechaFinalizacion] IS NULL AND [FechaCancelacion] IS NOT NULL)),
    CONSTRAINT [CK_SERVICIOS_Fechas] CHECK (([FechaEntregaReal] IS NULL OR [FechaEntregaReal] >= [FechaAsignacion]) AND ([FechaRetiroReal] IS NULL OR [FechaRetiroReal] >= [FechaEntregaReal]) AND ([FechaFinalizacion] IS NULL OR [FechaFinalizacion] >= [FechaRetiroReal]) AND ([FechaCancelacion] IS NULL OR [FechaCancelacion] >= [FechaAsignacion])),
    CONSTRAINT [CK_SERVICIOS_Frecuencia] CHECK ([CantidadLimpiezasSemana] >= 0),
    CONSTRAINT [CK_SERVICIOS_TipoUso] CHECK ([TipoUso] IN (N'Obra', N'Evento')),
    CONSTRAINT [FK_SERVICIOS_BANOS_QUIMICOS_IdBano] FOREIGN KEY ([IdBano]) REFERENCES [BANOS_QUIMICOS] ([IdBano]),
    CONSTRAINT [FK_SERVICIOS_CLIENTES_IdCliente] FOREIGN KEY ([IdCliente]) REFERENCES [CLIENTES] ([IdCliente])
);

CREATE TABLE [SOLICITUDES_CLIENTE] (
    [IdSolicitud] int NOT NULL IDENTITY,
    [IdCliente] int NOT NULL,
    [IdTipoOperacion] int NOT NULL,
    [IdEstadoOperacion] int NOT NULL,
    [FechaSolicitud] datetimeoffset NOT NULL,
    [IdServicioReferencia] int NULL,
    [Descripcion] nvarchar(2000) NULL,
    [CantidadBanos] int NULL,
    [IdTipoBanoSolicitado] int NULL,
    [TipoUsoSolicitado] nvarchar(10) NULL,
    [DireccionSolicitada] nvarchar(300) NULL,
    [FechaDeseada] datetimeoffset NULL,
    [NuevaDireccion] nvarchar(300) NULL,
    [ModalidadCambioDireccion] nvarchar(20) NULL,
    [FechaCambioEfectiva] datetimeoffset NULL,
    [FechaValidacion] datetimeoffset NULL,
    [FechaResolucion] datetimeoffset NULL,
    [ObservacionesAdministrador] nvarchar(2000) NULL,
    CONSTRAINT [PK_SOLICITUDES_CLIENTE] PRIMARY KEY ([IdSolicitud]),
    CONSTRAINT [CK_SOLICITUDES_CLIENTE_Cantidad] CHECK ([CantidadBanos] IS NULL OR [CantidadBanos] > 0),
    CONSTRAINT [CK_SOLICITUDES_CLIENTE_Modalidad] CHECK ([ModalidadCambioDireccion] IS NULL OR [ModalidadCambioDireccion] IN (N'Solicitado', N'Informado')),
    CONSTRAINT [CK_SOLICITUDES_CLIENTE_Uso] CHECK ([TipoUsoSolicitado] IS NULL OR [TipoUsoSolicitado] IN (N'Obra', N'Evento')),
    CONSTRAINT [FK_SOLICITUDES_CLIENTE_CLIENTES_IdCliente] FOREIGN KEY ([IdCliente]) REFERENCES [CLIENTES] ([IdCliente]),
    CONSTRAINT [FK_SOLICITUDES_CLIENTE_ESTADOS_OPERACION_IdEstadoOperacion] FOREIGN KEY ([IdEstadoOperacion]) REFERENCES [ESTADOS_OPERACION] ([IdEstadoOperacion]),
    CONSTRAINT [FK_SOLICITUDES_CLIENTE_SERVICIOS_IdServicioReferencia] FOREIGN KEY ([IdServicioReferencia]) REFERENCES [SERVICIOS] ([IdServicio]),
    CONSTRAINT [FK_SOLICITUDES_CLIENTE_TIPOS_BANO_IdTipoBanoSolicitado] FOREIGN KEY ([IdTipoBanoSolicitado]) REFERENCES [TIPOS_BANO] ([IdTipoBano]),
    CONSTRAINT [FK_SOLICITUDES_CLIENTE_TIPOS_OPERACION_IdTipoOperacion] FOREIGN KEY ([IdTipoOperacion]) REFERENCES [TIPOS_OPERACION] ([IdTipoOperacion])
);

CREATE TABLE [UBICACIONES_SERVICIO] (
    [IdUbicacion] int NOT NULL IDENTITY,
    [IdServicio] int NOT NULL,
    [Direccion] nvarchar(300) NOT NULL,
    [Localidad] nvarchar(100) NULL,
    [Referencia] nvarchar(500) NULL,
    [FechaRegistro] datetimeoffset NOT NULL,
    [EsActual] bit NOT NULL,
    [FechaDesde] datetimeoffset NULL,
    [FechaHasta] datetimeoffset NULL,
    [FechaValidacion] datetimeoffset NULL,
    CONSTRAINT [PK_UBICACIONES_SERVICIO] PRIMARY KEY ([IdUbicacion]),
    CONSTRAINT [CK_UBICACIONES_SERVICIO_Direccion] CHECK (LEN(LTRIM(RTRIM([Direccion]))) > 0),
    CONSTRAINT [CK_UBICACIONES_SERVICIO_Intervalo] CHECK (([FechaHasta] IS NULL OR ([FechaDesde] IS NOT NULL AND [FechaHasta] >= [FechaDesde])) AND ([EsActual] = 0 OR ([FechaDesde] IS NOT NULL AND [FechaHasta] IS NULL))),
    CONSTRAINT [FK_UBICACIONES_SERVICIO_SERVICIOS_IdServicio] FOREIGN KEY ([IdServicio]) REFERENCES [SERVICIOS] ([IdServicio])
);

CREATE TABLE [TAREAS_OPERATIVAS] (
    [IdTarea] int NOT NULL IDENTITY,
    [IdBano] int NOT NULL,
    [IdTipoOperacion] int NOT NULL,
    [IdEstadoOperacion] int NOT NULL,
    [FechaRegistro] datetimeoffset NOT NULL,
    [BloqueaAsignacion] bit NOT NULL,
    [IdServicio] int NULL,
    [IdEmpleado] int NULL,
    [IdSolicitud] int NULL,
    [IdUbicacion] int NULL,
    [IdUbicacionDestino] int NULL,
    [FechaProgramada] datetimeoffset NULL,
    [FechaInicioReal] datetimeoffset NULL,
    [FechaRealizada] datetimeoffset NULL,
    [FechaCancelacion] datetimeoffset NULL,
    [PapelHigienicoRepuesto] bit NULL,
    [ObservacionesEmpleado] nvarchar(2000) NULL,
    CONSTRAINT [PK_TAREAS_OPERATIVAS] PRIMARY KEY ([IdTarea]),
    CONSTRAINT [CK_TAREAS_OPERATIVAS_Bloqueo] CHECK ([BloqueaAsignacion] = 0 OR [IdTipoOperacion] = 11),
    CONSTRAINT [CK_TAREAS_OPERATIVAS_Empleado] CHECK ([IdEstadoOperacion] NOT IN (7, 8) OR [IdEmpleado] IS NOT NULL),
    CONSTRAINT [CK_TAREAS_OPERATIVAS_Servicio] CHECK ([IdServicio] IS NOT NULL OR [IdTipoOperacion] = 11),
    CONSTRAINT [FK_TAREAS_OPERATIVAS_BANOS_QUIMICOS_IdBano] FOREIGN KEY ([IdBano]) REFERENCES [BANOS_QUIMICOS] ([IdBano]),
    CONSTRAINT [FK_TAREAS_OPERATIVAS_ESTADOS_OPERACION_IdEstadoOperacion] FOREIGN KEY ([IdEstadoOperacion]) REFERENCES [ESTADOS_OPERACION] ([IdEstadoOperacion]),
    CONSTRAINT [FK_TAREAS_OPERATIVAS_SERVICIOS_IdServicio] FOREIGN KEY ([IdServicio]) REFERENCES [SERVICIOS] ([IdServicio]),
    CONSTRAINT [FK_TAREAS_OPERATIVAS_SOLICITUDES_CLIENTE_IdSolicitud] FOREIGN KEY ([IdSolicitud]) REFERENCES [SOLICITUDES_CLIENTE] ([IdSolicitud]),
    CONSTRAINT [FK_TAREAS_OPERATIVAS_TIPOS_OPERACION_IdTipoOperacion] FOREIGN KEY ([IdTipoOperacion]) REFERENCES [TIPOS_OPERACION] ([IdTipoOperacion]),
    CONSTRAINT [FK_TAREAS_OPERATIVAS_UBICACIONES_SERVICIO_IdUbicacion] FOREIGN KEY ([IdUbicacion]) REFERENCES [UBICACIONES_SERVICIO] ([IdUbicacion]),
    CONSTRAINT [FK_TAREAS_OPERATIVAS_UBICACIONES_SERVICIO_IdUbicacionDestino] FOREIGN KEY ([IdUbicacionDestino]) REFERENCES [UBICACIONES_SERVICIO] ([IdUbicacion]),
    CONSTRAINT [FK_TAREAS_OPERATIVAS_USUARIOS_IdEmpleado] FOREIGN KEY ([IdEmpleado]) REFERENCES [USUARIOS] ([IdUsuario])
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdEstadoBano', N'Descripcion', N'NombreEstado') AND [object_id] = OBJECT_ID(N'[ESTADOS_BANO]'))
    SET IDENTITY_INSERT [ESTADOS_BANO] ON;
INSERT INTO [ESTADOS_BANO] ([IdEstadoBano], [Descripcion], [NombreEstado])
VALUES (1, NULL, N'Apto'),
(2, NULL, N'No apto');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdEstadoBano', N'Descripcion', N'NombreEstado') AND [object_id] = OBJECT_ID(N'[ESTADOS_BANO]'))
    SET IDENTITY_INSERT [ESTADOS_BANO] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdEstadoOperacion', N'Ambito', N'Descripcion', N'NombreEstado') AND [object_id] = OBJECT_ID(N'[ESTADOS_OPERACION]'))
    SET IDENTITY_INSERT [ESTADOS_OPERACION] ON;
INSERT INTO [ESTADOS_OPERACION] ([IdEstadoOperacion], [Ambito], [Descripcion], [NombreEstado])
VALUES (1, N'Solicitud', NULL, N'Pendiente'),
(2, N'Solicitud', NULL, N'Aprobada'),
(3, N'Solicitud', NULL, N'Rechazada'),
(4, N'Solicitud', NULL, N'Resuelta'),
(5, N'Solicitud', NULL, N'Cancelada'),
(6, N'Tarea', NULL, N'Pendiente'),
(7, N'Tarea', NULL, N'En curso'),
(8, N'Tarea', NULL, N'Realizada'),
(9, N'Tarea', NULL, N'Cancelada');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdEstadoOperacion', N'Ambito', N'Descripcion', N'NombreEstado') AND [object_id] = OBJECT_ID(N'[ESTADOS_OPERACION]'))
    SET IDENTITY_INSERT [ESTADOS_OPERACION] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdRol', N'NombreRol') AND [object_id] = OBJECT_ID(N'[ROLES]'))
    SET IDENTITY_INSERT [ROLES] ON;
INSERT INTO [ROLES] ([IdRol], [NombreRol])
VALUES (1, N'Administrador'),
(2, N'Empleado'),
(3, N'Cliente');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdRol', N'NombreRol') AND [object_id] = OBJECT_ID(N'[ROLES]'))
    SET IDENTITY_INSERT [ROLES] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdTipoBano', N'Descripcion', N'NombreTipo') AND [object_id] = OBJECT_ID(N'[TIPOS_BANO]'))
    SET IDENTITY_INSERT [TIPOS_BANO] ON;
INSERT INTO [TIPOS_BANO] ([IdTipoBano], [Descripcion], [NombreTipo])
VALUES (1, NULL, N'Obra'),
(2, NULL, N'Evento');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdTipoBano', N'Descripcion', N'NombreTipo') AND [object_id] = OBJECT_ID(N'[TIPOS_BANO]'))
    SET IDENTITY_INSERT [TIPOS_BANO] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdTipoOperacion', N'AplicaASolicitudes', N'AplicaATareas', N'Descripcion', N'NombreOperacion') AND [object_id] = OBJECT_ID(N'[TIPOS_OPERACION]'))
    SET IDENTITY_INSERT [TIPOS_OPERACION] ON;
INSERT INTO [TIPOS_OPERACION] ([IdTipoOperacion], [AplicaASolicitudes], [AplicaATareas], [Descripcion], [NombreOperacion])
VALUES (1, CAST(1 AS bit), CAST(0 AS bit), NULL, N'Nuevo servicio'),
(2, CAST(1 AS bit), CAST(0 AS bit), NULL, N'Baño adicional'),
(3, CAST(1 AS bit), CAST(1 AS bit), NULL, N'Limpieza extra'),
(4, CAST(1 AS bit), CAST(0 AS bit), NULL, N'Cambio de dirección'),
(5, CAST(1 AS bit), CAST(0 AS bit), NULL, N'Reclamo'),
(6, CAST(1 AS bit), CAST(0 AS bit), NULL, N'Baja'),
(7, CAST(0 AS bit), CAST(1 AS bit), NULL, N'Entrega'),
(8, CAST(0 AS bit), CAST(1 AS bit), NULL, N'Retiro'),
(9, CAST(0 AS bit), CAST(1 AS bit), NULL, N'Limpieza semanal'),
(10, CAST(0 AS bit), CAST(1 AS bit), NULL, N'Traslado'),
(11, CAST(0 AS bit), CAST(1 AS bit), NULL, N'Mantenimiento');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdTipoOperacion', N'AplicaASolicitudes', N'AplicaATareas', N'Descripcion', N'NombreOperacion') AND [object_id] = OBJECT_ID(N'[TIPOS_OPERACION]'))
    SET IDENTITY_INSERT [TIPOS_OPERACION] OFF;

CREATE UNIQUE INDEX [IX_BANOS_QUIMICOS_CodigoInterno] ON [BANOS_QUIMICOS] ([CodigoInterno]);

CREATE INDEX [IX_BANOS_QUIMICOS_IdEstadoBano] ON [BANOS_QUIMICOS] ([IdEstadoBano]);

CREATE INDEX [IX_BANOS_QUIMICOS_IdTipoBano] ON [BANOS_QUIMICOS] ([IdTipoBano]);

CREATE UNIQUE INDEX [IX_CLIENTES_IdUsuario] ON [CLIENTES] ([IdUsuario]);

CREATE UNIQUE INDEX [IX_ESTADOS_BANO_NombreEstado] ON [ESTADOS_BANO] ([NombreEstado]);

CREATE UNIQUE INDEX [IX_ESTADOS_OPERACION_Ambito_NombreEstado] ON [ESTADOS_OPERACION] ([Ambito], [NombreEstado]);

CREATE UNIQUE INDEX [IX_ROLES_NombreRol] ON [ROLES] ([NombreRol]);

CREATE INDEX [IX_SERVICIOS_IdCliente] ON [SERVICIOS] ([IdCliente]);

CREATE INDEX [IX_SERVICIOS_IdSolicitudOrigen] ON [SERVICIOS] ([IdSolicitudOrigen]);

CREATE UNIQUE INDEX [UX_SERVICIOS_BanoAsignado] ON [SERVICIOS] ([IdBano]) WHERE [EstadoServicio] IN (N'Pendiente de entrega', N'Entregado');

CREATE INDEX [IX_SOLICITUDES_CLIENTE_IdCliente] ON [SOLICITUDES_CLIENTE] ([IdCliente]);

CREATE INDEX [IX_SOLICITUDES_CLIENTE_IdEstadoOperacion] ON [SOLICITUDES_CLIENTE] ([IdEstadoOperacion]);

CREATE INDEX [IX_SOLICITUDES_CLIENTE_IdServicioReferencia] ON [SOLICITUDES_CLIENTE] ([IdServicioReferencia]);

CREATE INDEX [IX_SOLICITUDES_CLIENTE_IdTipoBanoSolicitado] ON [SOLICITUDES_CLIENTE] ([IdTipoBanoSolicitado]);

CREATE INDEX [IX_SOLICITUDES_CLIENTE_IdTipoOperacion] ON [SOLICITUDES_CLIENTE] ([IdTipoOperacion]);

CREATE INDEX [IX_TAREAS_OPERATIVAS_IdBano_BloqueaAsignacion_IdEstadoOperacion] ON [TAREAS_OPERATIVAS] ([IdBano], [BloqueaAsignacion], [IdEstadoOperacion]);

CREATE INDEX [IX_TAREAS_OPERATIVAS_IdEmpleado] ON [TAREAS_OPERATIVAS] ([IdEmpleado]);

CREATE INDEX [IX_TAREAS_OPERATIVAS_IdEstadoOperacion] ON [TAREAS_OPERATIVAS] ([IdEstadoOperacion]);

CREATE INDEX [IX_TAREAS_OPERATIVAS_IdServicio] ON [TAREAS_OPERATIVAS] ([IdServicio]);

CREATE INDEX [IX_TAREAS_OPERATIVAS_IdSolicitud] ON [TAREAS_OPERATIVAS] ([IdSolicitud]);

CREATE INDEX [IX_TAREAS_OPERATIVAS_IdTipoOperacion] ON [TAREAS_OPERATIVAS] ([IdTipoOperacion]);

CREATE INDEX [IX_TAREAS_OPERATIVAS_IdUbicacion] ON [TAREAS_OPERATIVAS] ([IdUbicacion]);

CREATE INDEX [IX_TAREAS_OPERATIVAS_IdUbicacionDestino] ON [TAREAS_OPERATIVAS] ([IdUbicacionDestino]);

CREATE UNIQUE INDEX [IX_TIPOS_BANO_NombreTipo] ON [TIPOS_BANO] ([NombreTipo]);

CREATE UNIQUE INDEX [IX_TIPOS_OPERACION_NombreOperacion] ON [TIPOS_OPERACION] ([NombreOperacion]);

CREATE UNIQUE INDEX [UX_UBICACIONES_SERVICIO_Actual] ON [UBICACIONES_SERVICIO] ([IdServicio]) WHERE [EsActual] = 1;

CREATE UNIQUE INDEX [IX_USUARIOS_Email] ON [USUARIOS] ([Email]);

CREATE INDEX [IX_USUARIOS_IdRol] ON [USUARIOS] ([IdRol]);

ALTER TABLE [SERVICIOS] ADD CONSTRAINT [FK_SERVICIOS_SOLICITUDES_CLIENTE_IdSolicitudOrigen] FOREIGN KEY ([IdSolicitudOrigen]) REFERENCES [SOLICITUDES_CLIENTE] ([IdSolicitud]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261027203511_Inicial', N'10.0.12');

COMMIT;
GO

