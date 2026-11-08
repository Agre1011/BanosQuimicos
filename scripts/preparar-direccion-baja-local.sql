-- Tres escenarios independientes. No cambia cuentas, contraseñas ni trabajos anteriores.
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
IF DB_NAME()<>N'BanosQuimicosDb' OR ISNULL(CONVERT(int,SERVERPROPERTY('IsLocalDB')),0)<>1
    THROW 51000, 'Solo se permite BanosQuimicosDb en LocalDB.', 1;
DECLARE @Cliente int=(SELECT TOP(1) s.IdCliente FROM dbo.SERVICIOS s JOIN dbo.BANOS_QUIMICOS b ON b.IdBano=s.IdBano
    JOIN dbo.CLIENTES c ON c.IdCliente=s.IdCliente JOIN dbo.USUARIOS u ON u.IdUsuario=c.IdUsuario JOIN dbo.ROLES r ON r.IdRol=u.IdRol
    WHERE b.CodigoInterno=N'PRUEBA-TAREAS-01' AND r.NombreRol=N'Cliente' AND u.EstadoCuenta=N'Activa' ORDER BY s.IdServicio);
DECLARE @Empleado int=(SELECT u.IdUsuario FROM dbo.USUARIOS u JOIN dbo.ROLES r ON r.IdRol=u.IdRol
    WHERE u.Email=N'prueba.cuentas.windows.01@example.invalid' AND r.NombreRol=N'Empleado' AND u.EstadoCuenta=N'Activa');
IF @Cliente IS NULL OR @Empleado IS NULL THROW 51000, 'Faltan cuentas locales conocidas y activas; no se crean ni modifican cuentas.', 1;
DECLARE @Tipo int=(SELECT IdTipoBano FROM dbo.TIPOS_BANO WHERE NombreTipo=N'Obra');
DECLARE @Apto int=(SELECT IdEstadoBano FROM dbo.ESTADOS_BANO WHERE NombreEstado=N'Apto');
DECLARE @Ahora datetimeoffset=SYSDATETIMEOFFSET();
DECLARE @Marca nvarchar(2000)=N'PRUEBA DIRECCION BAJA CLIENTE - SOLO LOCAL';
DECLARE @Ejemplos TABLE(Numero int,Codigo nvarchar(30) COLLATE Latin1_General_100_CI_AS);
INSERT @Ejemplos VALUES(1,N'PRUEBA-DIR-SOL'),(2,N'PRUEBA-DIR-INF'),(3,N'PRUEBA-BAJA-CLI');
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS(SELECT 1 FROM dbo.BANOS_QUIMICOS b JOIN @Ejemplos e ON e.Codigo=b.CodigoInterno WHERE ISNULL(b.Observaciones,N'')<>@Marca)
        THROW 51000, 'Un código pertenece a otro registro; no se modifica.', 1;
    INSERT dbo.BANOS_QUIMICOS(CodigoInterno,IdTipoBano,IdEstadoBano,FechaAlta,Observaciones,IdResponsableLimpieza)
    SELECT e.Codigo,@Tipo,@Apto,DATEADD(day,-4,@Ahora),@Marca,@Empleado FROM @Ejemplos e
    WHERE NOT EXISTS(SELECT 1 FROM dbo.BANOS_QUIMICOS b WHERE b.CodigoInterno=e.Codigo);
    DECLARE @Nuevos TABLE(IdServicio int,IdBano int);
    INSERT dbo.SERVICIOS(IdCliente,IdBano,TipoUso,CantidadLimpiezasSemana,EstadoServicio,FechaAsignacion,FechaEntregaReal,Observaciones)
    OUTPUT inserted.IdServicio,inserted.IdBano INTO @Nuevos
    SELECT @Cliente,b.IdBano,N'Obra',1,N'Entregado',DATEADD(day,-3,@Ahora),DATEADD(day,-2,@Ahora),@Marca
    FROM dbo.BANOS_QUIMICOS b JOIN @Ejemplos e ON e.Codigo=b.CodigoInterno
    WHERE NOT EXISTS(SELECT 1 FROM dbo.SERVICIOS s WHERE s.IdBano=b.IdBano AND s.Observaciones=@Marca);
    INSERT dbo.UBICACIONES_SERVICIO(IdServicio,Direccion,FechaRegistro,EsActual,FechaDesde)
    SELECT n.IdServicio,N'PRUEBA LOCAL - Origen '+e.Codigo+N' 123',DATEADD(day,-2,@Ahora),1,DATEADD(day,-2,@Ahora)
    FROM @Nuevos n JOIN dbo.BANOS_QUIMICOS b ON b.IdBano=n.IdBano JOIN @Ejemplos e ON e.Codigo=b.CodigoInterno;
    INSERT dbo.TAREAS_OPERATIVAS(IdBano,IdTipoOperacion,IdEstadoOperacion,FechaRegistro,BloqueaAsignacion,IdServicio,IdEmpleado,IdUbicacion,FechaProgramada)
    SELECT n.IdBano,CASE WHEN e.Numero=3 AND a.Numero=2 THEN 11 ELSE 9 END,6,@Ahora,
        CASE WHEN e.Numero=3 AND a.Numero=2 THEN 1 ELSE 0 END,n.IdServicio,@Empleado,u.IdUbicacion,DATEADD(day,1,@Ahora)
    FROM @Nuevos n JOIN dbo.BANOS_QUIMICOS b ON b.IdBano=n.IdBano JOIN @Ejemplos e ON e.Codigo=b.CodigoInterno
    JOIN dbo.UBICACIONES_SERVICIO u ON u.IdServicio=n.IdServicio AND u.EsActual=1 CROSS JOIN (VALUES(1),(2)) a(Numero);
    COMMIT;
    SELECT b.CodigoInterno,s.IdServicio,s.EstadoServicio,u.Direccion,u.FechaDesde
    FROM dbo.BANOS_QUIMICOS b JOIN @Ejemplos e ON e.Codigo=b.CodigoInterno JOIN dbo.SERVICIOS s ON s.IdBano=b.IdBano AND s.Observaciones=@Marca
    LEFT JOIN dbo.UBICACIONES_SERVICIO u ON u.IdServicio=s.IdServicio AND u.EsActual=1 ORDER BY e.Numero;
    SELECT b.CodigoInterno,t.IdTarea,t.IdTipoOperacion,t.IdEstadoOperacion,t.BloqueaAsignacion
    FROM dbo.TAREAS_OPERATIVAS t JOIN dbo.BANOS_QUIMICOS b ON b.IdBano=t.IdBano JOIN @Ejemplos e ON e.Codigo=b.CodigoInterno ORDER BY e.Numero,t.IdTarea;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
-- Repetir conserva servicios finalizados, ubicaciones y tareas: solo agrega los escenarios que aún no existen.
