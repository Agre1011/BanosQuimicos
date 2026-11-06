-- Preparación MANUAL de una tarea de prueba, solo en LocalDB/BanosQuimicosDb.
-- Crear primero estas dos cuentas desde la app local. Activar ambas y dar rol Empleado a la primera.
-- Se pueden cambiar los correos de prueba siguientes. No se crean usuarios ni contraseñas por SQL.
-- Ejecutar como un único lote, sin añadir GO entre las comprobaciones.
SET XACT_ABORT ON;
IF DB_NAME() <> N'BanosQuimicosDb'
    THROW 51000, 'Seleccionar BanosQuimicosDb local. No ejecutar en hosting.', 1;
IF ISNULL(CONVERT(int, SERVERPROPERTY('IsLocalDB')), 0) <> 1
    THROW 51000, 'Este script solo permite la instancia MSSQLLocalDB.', 1;

DECLARE @CorreoEmpleado nvarchar(254) = N'prueba.tareas.empleado@example.invalid';
DECLARE @CorreoCliente nvarchar(254) = N'prueba.tareas.cliente@example.invalid';
DECLARE @Empleado int, @Cliente int;
SELECT @Empleado = u.IdUsuario FROM dbo.USUARIOS u JOIN dbo.ROLES r ON r.IdRol = u.IdRol
WHERE u.Email = @CorreoEmpleado AND u.EstadoCuenta = N'Activa' AND r.NombreRol = N'Empleado';
SELECT @Cliente = c.IdCliente FROM dbo.CLIENTES c JOIN dbo.USUARIOS u ON u.IdUsuario = c.IdUsuario
JOIN dbo.ROLES r ON r.IdRol = u.IdRol
WHERE u.Email = @CorreoCliente AND u.EstadoCuenta = N'Activa' AND r.NombreRol = N'Cliente';
IF @Empleado IS NULL OR @Cliente IS NULL
    THROW 51000, 'Faltan las cuentas de prueba activas y con los roles indicados. No se modificó nada.', 1;

DECLARE @Marca nvarchar(2000) = N'PRUEBA CONSULTA TAREAS - SOLO LOCAL';
DECLARE @Codigo nvarchar(30) = N'PRUEBA-TAREAS-01';
DECLARE @TipoBano int = (SELECT IdTipoBano FROM dbo.TIPOS_BANO WHERE NombreTipo = N'Obra');
DECLARE @Apto int = (SELECT IdEstadoBano FROM dbo.ESTADOS_BANO WHERE NombreEstado = N'Apto');
DECLARE @TipoTarea int = (SELECT IdTipoOperacion FROM dbo.TIPOS_OPERACION WHERE NombreOperacion = N'Limpieza extra' AND AplicaATareas = 1);
DECLARE @Pendiente int = (SELECT IdEstadoOperacion FROM dbo.ESTADOS_OPERACION WHERE NombreEstado = N'Pendiente' AND Ambito = N'Tarea');
DECLARE @Ahora datetimeoffset = SYSDATETIMEOFFSET();
DECLARE @Hoy date = CONVERT(date, SWITCHOFFSET(@Ahora, '-03:00'));
DECLARE @Programada datetimeoffset = TODATETIMEOFFSET(DATEADD(hour, 10, CONVERT(datetime2, @Hoy)), '-03:00');
DECLARE @Bano int, @Servicio int, @Ubicacion int, @Tarea int;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM dbo.BANOS_QUIMICOS WHERE CodigoInterno = @Codigo AND ISNULL(Observaciones, N'') <> @Marca)
        THROW 51000, 'El código de prueba ya pertenece a otro registro. No se modifica.', 1;
    SELECT @Bano = IdBano FROM dbo.BANOS_QUIMICOS WHERE CodigoInterno = @Codigo;
    IF @Bano IS NULL
    BEGIN
        INSERT dbo.BANOS_QUIMICOS (CodigoInterno, IdTipoBano, IdEstadoBano, FechaAlta, Observaciones)
        VALUES (@Codigo, @TipoBano, @Apto, DATEADD(day,-2,@Ahora), @Marca);
        SET @Bano = CONVERT(int, SCOPE_IDENTITY());
    END;
    SELECT @Servicio = IdServicio FROM dbo.SERVICIOS
    WHERE IdBano = @Bano AND IdCliente = @Cliente AND EstadoServicio = N'Entregado' AND Observaciones = @Marca;
    IF @Servicio IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.SERVICIOS WHERE IdBano = @Bano AND EstadoServicio IN (N'Entregado', N'Pendiente de entrega'))
            THROW 51000, 'El baño de prueba ya tiene otra asignación vigente. No se modifica.', 1;
        INSERT dbo.SERVICIOS (IdCliente, IdBano, TipoUso, CantidadLimpiezasSemana, EstadoServicio, FechaAsignacion, FechaEntregaReal, Observaciones)
        VALUES (@Cliente, @Bano, N'Obra', 1, N'Entregado', DATEADD(day,-2,@Ahora), DATEADD(day,-1,@Ahora), @Marca);
        SET @Servicio = CONVERT(int, SCOPE_IDENTITY());
    END;
    SELECT @Ubicacion = IdUbicacion FROM dbo.UBICACIONES_SERVICIO WHERE IdServicio = @Servicio AND EsActual = 1;
    IF @Ubicacion IS NULL
    BEGIN
        INSERT dbo.UBICACIONES_SERVICIO (IdServicio, Direccion, FechaRegistro, EsActual, FechaDesde)
        VALUES (@Servicio, N'PRUEBA LOCAL - Av. de ejemplo 123', @Ahora, 1, DATEADD(day,-1,@Ahora));
        SET @Ubicacion = CONVERT(int, SCOPE_IDENTITY());
    END;
    SELECT TOP(1) @Tarea = IdTarea FROM dbo.TAREAS_OPERATIVAS
    WHERE IdEmpleado = @Empleado AND IdServicio = @Servicio AND IdUbicacion = @Ubicacion
      AND IdTipoOperacion = @TipoTarea AND FechaProgramada = @Programada ORDER BY IdTarea;
    IF @Tarea IS NULL
    BEGIN
        INSERT dbo.TAREAS_OPERATIVAS (IdBano, IdTipoOperacion, IdEstadoOperacion, FechaRegistro, BloqueaAsignacion, IdServicio, IdEmpleado, IdUbicacion, FechaProgramada)
        VALUES (@Bano, @TipoTarea, @Pendiente, @Ahora, 0, @Servicio, @Empleado, @Ubicacion, @Programada);
        SET @Tarea = CONVERT(int, SCOPE_IDENTITY());
    END;
    COMMIT;
    SELECT @Tarea AS IdTarea, @Empleado AS IdEmpleado, @Codigo AS CodigoBano, @Programada AS FechaProgramada;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
