-- VALIDADO PARA REVISION - NO EJECUTADO. EF Core 10.0.12, 09/11/2026.
-- Destino: MonsterASP / db77593. Historial confirmado por el estudiante el 09/11/2026:
-- solo 20261027203511_Inicial, ProductVersion 10.0.12. Sin consulta remota directa.
-- Rango: 20261027203511_Inicial -> 20261108193729_RegistroClienteActivo.
-- Inicial debe existir y el historial/esquema deben ser coherentes antes de usarlo.
-- Es idempotente por MigrationId: omite las migraciones ya registradas.
-- No sustituye la comprobacion del historial, la revision ni el respaldo previo.
-- No usar en una base vacia ni ejecutar de nuevo db77593-inicial.sql sobre datos existentes.
-- Solo agrega campos opcionales/indices/FK/CHECK y cambia DEFAULT EstadoCuenta a Activa.
-- DROP CONSTRAINT retira solo el DEFAULT anterior, no elimina cuentas ni cambia sus estados.
-- Los INSERT se limitan a __EFMigrationsHistory. No incluye datos de LocalDB.
-- Cada migracion tiene su propia transaccion: un fallo no revierte las ya confirmadas.
-- Usar una herramienta compatible con GO y detenerse ante el primer error.

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106204848_RegistroTrabajoTarea'
)
BEGIN
    ALTER TABLE [TAREAS_OPERATIVAS] ADD [FechaTrabajoRegistrado] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106204848_RegistroTrabajoTarea'
)
BEGIN
    ALTER TABLE [TAREAS_OPERATIVAS] ADD [IdEmpleadoRegistroTrabajo] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106204848_RegistroTrabajoTarea'
)
BEGIN
    CREATE INDEX [IX_TAREAS_OPERATIVAS_IdEmpleadoRegistroTrabajo] ON [TAREAS_OPERATIVAS] ([IdEmpleadoRegistroTrabajo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106204848_RegistroTrabajoTarea'
)
BEGIN
    EXEC(N'ALTER TABLE [TAREAS_OPERATIVAS] ADD CONSTRAINT [CK_TAREAS_OPERATIVAS_RegistroTrabajo] CHECK (([FechaTrabajoRegistrado] IS NULL AND [IdEmpleadoRegistroTrabajo] IS NULL) OR ([FechaTrabajoRegistrado] IS NOT NULL AND [IdEmpleadoRegistroTrabajo] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106204848_RegistroTrabajoTarea'
)
BEGIN
    ALTER TABLE [TAREAS_OPERATIVAS] ADD CONSTRAINT [FK_TAREAS_OPERATIVAS_USUARIOS_IdEmpleadoRegistroTrabajo] FOREIGN KEY ([IdEmpleadoRegistroTrabajo]) REFERENCES [USUARIOS] ([IdUsuario]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106204848_RegistroTrabajoTarea'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106204848_RegistroTrabajoTarea', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106222844_ResponsableLimpiezaYProgramacion'
)
BEGIN
    ALTER TABLE [TAREAS_OPERATIVAS] ADD [ClaveProgramacion] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106222844_ResponsableLimpiezaYProgramacion'
)
BEGIN
    ALTER TABLE [BANOS_QUIMICOS] ADD [IdResponsableLimpieza] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106222844_ResponsableLimpiezaYProgramacion'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TAREAS_OPERATIVAS_ClaveProgramacion] ON [TAREAS_OPERATIVAS] ([ClaveProgramacion]) WHERE [ClaveProgramacion] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106222844_ResponsableLimpiezaYProgramacion'
)
BEGIN
    CREATE INDEX [IX_BANOS_QUIMICOS_IdResponsableLimpieza] ON [BANOS_QUIMICOS] ([IdResponsableLimpieza]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106222844_ResponsableLimpiezaYProgramacion'
)
BEGIN
    ALTER TABLE [BANOS_QUIMICOS] ADD CONSTRAINT [FK_BANOS_QUIMICOS_USUARIOS_IdResponsableLimpieza] FOREIGN KEY ([IdResponsableLimpieza]) REFERENCES [USUARIOS] ([IdUsuario]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106222844_ResponsableLimpiezaYProgramacion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106222844_ResponsableLimpiezaYProgramacion', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107191916_ClaveEnvioSolicitud'
)
BEGIN
    ALTER TABLE [SOLICITUDES_CLIENTE] ADD [ClaveEnvio] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107191916_ClaveEnvioSolicitud'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SOLICITUDES_CLIENTE_ClaveEnvio] ON [SOLICITUDES_CLIENTE] ([ClaveEnvio]) WHERE [ClaveEnvio] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107191916_ClaveEnvioSolicitud'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107191916_ClaveEnvioSolicitud', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107211946_RevisionUbicacionSolicitud'
)
BEGIN
    ALTER TABLE [SOLICITUDES_CLIENTE] ADD [RevisionUbicacionJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107211946_RevisionUbicacionSolicitud'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107211946_RevisionUbicacionSolicitud', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108174906_InventarioBanosYMantenimiento'
)
BEGIN
    ALTER TABLE [TAREAS_OPERATIVAS] ADD [DescripcionTrabajo] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108174906_InventarioBanosYMantenimiento'
)
BEGIN
    ALTER TABLE [BANOS_QUIMICOS] ADD [ClaveAlta] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108174906_InventarioBanosYMantenimiento'
)
BEGIN
    ALTER TABLE [BANOS_QUIMICOS] ADD [FechaBaja] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108174906_InventarioBanosYMantenimiento'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BANOS_QUIMICOS_ClaveAlta] ON [BANOS_QUIMICOS] ([ClaveAlta]) WHERE [ClaveAlta] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108174906_InventarioBanosYMantenimiento'
)
BEGIN
    EXEC(N'ALTER TABLE [BANOS_QUIMICOS] ADD CONSTRAINT [CK_BANOS_QUIMICOS_FechaBaja] CHECK ([FechaBaja] IS NULL OR [FechaBaja] >= [FechaAlta])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108174906_InventarioBanosYMantenimiento'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108174906_InventarioBanosYMantenimiento', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108193729_RegistroClienteActivo'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[USUARIOS]') AND [c].[name] = N'EstadoCuenta');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [USUARIOS] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [USUARIOS] ADD DEFAULT N'Activa' FOR [EstadoCuenta];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108193729_RegistroClienteActivo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108193729_RegistroClienteActivo', N'10.0.12');
END;

COMMIT;
GO

