-- Dos escenarios nuevos de retiro. No modifica cuentas ni trabajos de bloques anteriores.
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
IF DB_NAME()<>N'BanosQuimicosDb' OR ISNULL(CONVERT(int,SERVERPROPERTY('IsLocalDB')),0)<>1
    THROW 51000, 'Solo se permite BanosQuimicosDb en LocalDB.', 1;
DECLARE @Cliente int=(SELECT TOP(1) s.IdCliente FROM dbo.SERVICIOS s JOIN dbo.BANOS_QUIMICOS b ON b.IdBano=s.IdBano
    WHERE b.CodigoInterno=N'PRUEBA-TAREAS-01' AND b.Observaciones=N'PRUEBA CONSULTA TAREAS - SOLO LOCAL' ORDER BY s.IdServicio);
DECLARE @Empleado int=(SELECT u.IdUsuario FROM dbo.USUARIOS u JOIN dbo.ROLES r ON r.IdRol=u.IdRol
    WHERE u.Email=N'prueba.cuentas.windows.01@example.invalid' AND r.NombreRol=N'Empleado' AND u.EstadoCuenta=N'Activa');
IF @Cliente IS NULL OR @Empleado IS NULL THROW 51000, 'Faltan cliente/empleado locales de prueba. No se crean cuentas.', 1;
DECLARE @Tipo int=(SELECT IdTipoBano FROM dbo.TIPOS_BANO WHERE NombreTipo=N'Obra');
DECLARE @Apto int=(SELECT IdEstadoBano FROM dbo.ESTADOS_BANO WHERE NombreEstado=N'Apto');
DECLARE @Ahora datetimeoffset=SYSDATETIMEOFFSET();
DECLARE @Marca nvarchar(2000)=N'PRUEBA BANOS Y SERVICIOS - SOLO LOCAL';
DECLARE @Ejemplos TABLE(Numero int,Codigo nvarchar(30) COLLATE Latin1_General_100_CI_AS);
INSERT @Ejemplos VALUES(1,N'PRUEBA-RETIRO-01'),(2,N'PRUEBA-RETIRO-02');
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS(SELECT 1 FROM dbo.BANOS_QUIMICOS b JOIN @Ejemplos e ON e.Codigo=b.CodigoInterno WHERE ISNULL(b.Observaciones,N'')<>@Marca)
        THROW 51000, 'Un código ya pertenece a otro registro. No se modifica.', 1;
    INSERT dbo.BANOS_QUIMICOS(CodigoInterno,IdTipoBano,IdEstadoBano,FechaAlta,Observaciones)
    SELECT Codigo,@Tipo,@Apto,DATEADD(day,-4,@Ahora),@Marca FROM @Ejemplos e
    WHERE NOT EXISTS(SELECT 1 FROM dbo.BANOS_QUIMICOS b WHERE b.CodigoInterno=e.Codigo);
    DECLARE @N int=1,@Bano int,@Servicio int,@Ubicacion int,@Codigo nvarchar(30),@Descripcion nvarchar(2000);
    WHILE @N<=2
    BEGIN
        SELECT @Codigo=Codigo FROM @Ejemplos WHERE Numero=@N;
        SELECT @Bano=IdBano FROM dbo.BANOS_QUIMICOS WHERE CodigoInterno=@Codigo;
        SET @Servicio=NULL; SET @Ubicacion=NULL;
        SELECT @Servicio=IdServicio FROM dbo.SERVICIOS WHERE IdBano=@Bano AND IdCliente=@Cliente AND Observaciones=@Marca;
        IF @Servicio IS NULL
        BEGIN
            IF EXISTS(SELECT 1 FROM dbo.SERVICIOS WHERE IdBano=@Bano AND EstadoServicio IN(N'Entregado',N'Pendiente de entrega'))
                THROW 51000, 'La unidad ya tiene otra asignación. No se modifica.', 1;
            INSERT dbo.SERVICIOS(IdCliente,IdBano,TipoUso,CantidadLimpiezasSemana,EstadoServicio,FechaAsignacion,FechaEntregaReal,Observaciones)
            VALUES(@Cliente,@Bano,N'Obra',1,N'Entregado',DATEADD(day,-3,@Ahora),DATEADD(day,-2,@Ahora),@Marca);
            SET @Servicio=CONVERT(int,SCOPE_IDENTITY());
        END;
        SELECT TOP(1) @Ubicacion=IdUbicacion FROM dbo.UBICACIONES_SERVICIO WHERE IdServicio=@Servicio ORDER BY EsActual DESC,IdUbicacion DESC;
        IF @Ubicacion IS NULL
        BEGIN
            INSERT dbo.UBICACIONES_SERVICIO(IdServicio,Direccion,FechaRegistro,EsActual,FechaDesde)
            VALUES(@Servicio,N'PRUEBA LOCAL - Retiro '+CONVERT(nvarchar(10),@N)+N' 123',DATEADD(day,-2,@Ahora),1,DATEADD(day,-2,@Ahora));
            SET @Ubicacion=CONVERT(int,SCOPE_IDENTITY());
        END;
        IF @N=1
        BEGIN
            SET @Descripcion=N'PRUEBA LOCAL BAJA - PRUEBA-RETIRO-01: retirar el servicio de ejemplo; revisar la limpieza futura antes del cierre.';
            IF NOT EXISTS(SELECT 1 FROM dbo.SOLICITUDES_CLIENTE WHERE IdServicioReferencia=@Servicio AND IdTipoOperacion=6 AND Descripcion=@Descripcion)
                INSERT dbo.SOLICITUDES_CLIENTE(IdCliente,IdServicioReferencia,IdTipoOperacion,IdEstadoOperacion,FechaSolicitud,Descripcion)
                VALUES(@Cliente,@Servicio,6,1,@Ahora,@Descripcion);
            IF NOT EXISTS(SELECT 1 FROM dbo.TAREAS_OPERATIVAS WHERE IdServicio=@Servicio AND IdTipoOperacion=9)
                INSERT dbo.TAREAS_OPERATIVAS(IdBano,IdServicio,IdUbicacion,IdEmpleado,IdTipoOperacion,IdEstadoOperacion,FechaRegistro,FechaProgramada,BloqueaAsignacion)
                VALUES(@Bano,@Servicio,@Ubicacion,@Empleado,9,6,@Ahora,DATEADD(day,7,@Ahora),0);
        END
        ELSE IF NOT EXISTS(SELECT 1 FROM dbo.TAREAS_OPERATIVAS WHERE IdServicio=@Servicio AND IdTipoOperacion=11 AND BloqueaAsignacion=1)
            INSERT dbo.TAREAS_OPERATIVAS(IdBano,IdServicio,IdUbicacion,IdEmpleado,IdTipoOperacion,IdEstadoOperacion,FechaRegistro,BloqueaAsignacion)
            VALUES(@Bano,@Servicio,@Ubicacion,@Empleado,11,6,@Ahora,1);
        SET @N=@N+1;
    END;
    COMMIT;
    SELECT s.IdServicio,b.CodigoInterno,s.EstadoServicio FROM dbo.SERVICIOS s JOIN dbo.BANOS_QUIMICOS b ON b.IdBano=s.IdBano
    JOIN @Ejemplos e ON e.Codigo=b.CodigoInterno WHERE s.Observaciones=@Marca ORDER BY e.Numero;
    SELECT t.IdTarea,b.CodigoInterno,o.NombreOperacion,t.BloqueaAsignacion FROM dbo.TAREAS_OPERATIVAS t
    JOIN dbo.BANOS_QUIMICOS b ON b.IdBano=t.IdBano JOIN @Ejemplos e ON e.Codigo=b.CodigoInterno
    JOIN dbo.TIPOS_OPERACION o ON o.IdTipoOperacion=t.IdTipoOperacion ORDER BY t.IdTarea;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
-- Repetir conserva los resultados de la prueba. No reabre servicios, tareas o solicitudes.
