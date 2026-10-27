-- Verificación de solo lectura: no inserta usuarios ni datos operativos.
USE [BanosQuimicosDb];
SET NOCOUNT ON;

DECLARE @tablas TABLE (Nombre sysname PRIMARY KEY);
INSERT INTO @tablas VALUES
    (N'USUARIOS'), (N'ROLES'), (N'CLIENTES'), (N'BANOS_QUIMICOS'),
    (N'TIPOS_BANO'), (N'ESTADOS_BANO'), (N'SERVICIOS'),
    (N'UBICACIONES_SERVICIO'), (N'SOLICITUDES_CLIENTE'),
    (N'TAREAS_OPERATIVAS'), (N'TIPOS_OPERACION'), (N'ESTADOS_OPERACION');

IF EXISTS (SELECT Nombre FROM @tablas EXCEPT SELECT name FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo'))
    THROW 51000, N'Faltan tablas del modelo.', 1;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo')) <> 13
    THROW 51001, N'Se esperaban 12 tablas de negocio y el historial de EF.', 1;
IF (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) <> 1
    THROW 51002, N'Se esperaba una migración inicial aplicada.', 1;

IF (SELECT COUNT(*) FROM dbo.ROLES) <> 3
    OR (SELECT COUNT(*) FROM dbo.TIPOS_BANO) <> 2
    OR (SELECT COUNT(*) FROM dbo.ESTADOS_BANO) <> 2
    OR (SELECT COUNT(*) FROM dbo.TIPOS_OPERACION) <> 11
    OR (SELECT COUNT(*) FROM dbo.ESTADOS_OPERACION) <> 9
    THROW 51003, N'Las cantidades de los catálogos no coinciden.', 1;

IF EXISTS (SELECT 1 FROM dbo.USUARIOS)
    OR EXISTS (SELECT 1 FROM dbo.CLIENTES)
    OR EXISTS (SELECT 1 FROM dbo.BANOS_QUIMICOS)
    OR EXISTS (SELECT 1 FROM dbo.SERVICIOS)
    OR EXISTS (SELECT 1 FROM dbo.UBICACIONES_SERVICIO)
    OR EXISTS (SELECT 1 FROM dbo.SOLICITUDES_CLIENTE)
    OR EXISTS (SELECT 1 FROM dbo.TAREAS_OPERATIVAS)
    THROW 51004, N'Esta comprobación inicial espera tablas operativas vacías.', 1;

IF (SELECT COUNT(*) FROM sys.foreign_keys) <> 21
    OR EXISTS (SELECT 1 FROM sys.foreign_keys WHERE delete_referential_action <> 0 OR is_disabled = 1 OR is_not_trusted = 1)
    THROW 51005, N'Revisar las 21 relaciones: deben estar activas y sin borrado en cascada.', 1;

DECLARE @indices TABLE (Tabla sysname, Nombre sysname);
INSERT INTO @indices VALUES
    (N'USUARIOS', N'IX_USUARIOS_Email'),
    (N'BANOS_QUIMICOS', N'IX_BANOS_QUIMICOS_CodigoInterno'),
    (N'CLIENTES', N'IX_CLIENTES_IdUsuario'),
    (N'SERVICIOS', N'UX_SERVICIOS_BanoAsignado'),
    (N'UBICACIONES_SERVICIO', N'UX_UBICACIONES_SERVICIO_Actual');
IF EXISTS (
    SELECT 1 FROM @indices esperados
    WHERE NOT EXISTS (
        SELECT 1 FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID(N'dbo.' + esperados.Tabla)
            AND i.name = esperados.Nombre AND i.is_unique = 1 AND i.is_disabled = 0))
    THROW 51006, N'Falta un índice único requerido.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SERVICIOS')
    AND name = N'UX_SERVICIOS_BanoAsignado' AND has_filter = 1
    AND filter_definition LIKE N'%Pendiente de entrega%' AND filter_definition LIKE N'%Entregado%')
    THROW 51007, N'El índice de asignación no incluye ambos estados vigentes.', 1;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.UBICACIONES_SERVICIO')
    AND name = N'UX_UBICACIONES_SERVICIO_Actual' AND has_filter = 1)
    THROW 51008, N'Falta el filtro de ubicación actual.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.USUARIOS')
    AND name = N'Email' AND collation_name = N'Latin1_General_100_CI_AS' AND is_nullable = 0)
    THROW 51009, N'El correo debe ser obligatorio y no distinguir mayúsculas.', 1;
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.USUARIOS')
    AND name = N'CK_USUARIOS_Email' AND is_disabled = 0 AND is_not_trusted = 0)
    THROW 51010, N'Falta la restricción de correo sin espacios exteriores.', 1;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE is_disabled = 1 OR is_not_trusted = 1)
    THROW 51011, N'Hay restricciones CHECK deshabilitadas o sin validar.', 1;

SELECT N'OK: esquema, catálogos, índices e historial verificados; sin datos operativos.' AS Resultado;
SELECT name AS Tabla FROM sys.tables ORDER BY name;
SELECT * FROM dbo.ROLES ORDER BY IdRol;
SELECT * FROM dbo.TIPOS_BANO ORDER BY IdTipoBano;
SELECT * FROM dbo.ESTADOS_BANO ORDER BY IdEstadoBano;
SELECT * FROM dbo.TIPOS_OPERACION ORDER BY IdTipoOperacion;
SELECT * FROM dbo.ESTADOS_OPERACION ORDER BY IdEstadoOperacion;
SELECT OBJECT_NAME(parent_object_id) AS Tabla, name AS Relacion, delete_referential_action_desc AS Borrado
    FROM sys.foreign_keys ORDER BY Tabla, Relacion;
SELECT t.name AS Tabla, i.name AS Indice, i.filter_definition AS Filtro
    FROM sys.indexes i INNER JOIN sys.tables t ON t.object_id = i.object_id
    WHERE i.is_unique = 1 AND i.is_primary_key = 0 ORDER BY Tabla, Indice;
SELECT * FROM dbo.__EFMigrationsHistory;
