-- Preparación identificable de este bloque, solo en LocalDB/BanosQuimicosDb.
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
IF DB_NAME()<>N'BanosQuimicosDb' OR ISNULL(CONVERT(int,SERVERPROPERTY('IsLocalDB')),0)<>1
    THROW 51000, 'Solo se permite BanosQuimicosDb en LocalDB.', 1;
DECLARE @Cliente int;
SELECT @Cliente=s.IdCliente FROM dbo.SERVICIOS s JOIN dbo.BANOS_QUIMICOS b ON b.IdBano=s.IdBano
WHERE b.CodigoInterno=N'PRUEBA-TAREAS-01' AND b.Observaciones=N'PRUEBA CONSULTA TAREAS - SOLO LOCAL';
IF @Cliente IS NULL THROW 51000, 'Falta el cliente del servicio de prueba existente. No se crean cuentas.', 1;
DECLARE @Tipo int=(SELECT IdTipoBano FROM dbo.TIPOS_BANO WHERE NombreTipo=N'Obra');
DECLARE @Apto int=(SELECT IdEstadoBano FROM dbo.ESTADOS_BANO WHERE NombreEstado=N'Apto');
DECLARE @Ahora datetimeoffset=SYSDATETIMEOFFSET();
DECLARE @Marca nvarchar(2000)=N'PRUEBA NUEVO SERVICIO - SOLO LOCAL';
DECLARE @Descripcion nvarchar(2000)=N'PRUEBA LOCAL SERVICIO NUEVO - DOS BANOS: dos unidades de tipo Obra para uso Evento. Datos ficticios identificables.';
-- Igualar solo esta variable de lote a la collation del código existente, sin cambiar la base.
DECLARE @Codigos TABLE(Codigo nvarchar(30) COLLATE Latin1_General_100_CI_AS);
INSERT @Codigos VALUES (N'PRUEBA-NUEVO-01'),(N'PRUEBA-NUEVO-02');
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS(SELECT 1 FROM dbo.BANOS_QUIMICOS b JOIN @Codigos c ON c.Codigo=b.CodigoInterno WHERE ISNULL(b.Observaciones,N'')<>@Marca)
        THROW 51000, 'Un código pertenece a otro registro; no se modifica.', 1;
    INSERT dbo.BANOS_QUIMICOS(CodigoInterno,IdTipoBano,IdEstadoBano,FechaAlta,Observaciones)
    SELECT c.Codigo,@Tipo,@Apto,@Ahora,@Marca FROM @Codigos c
    WHERE NOT EXISTS(SELECT 1 FROM dbo.BANOS_QUIMICOS b WHERE b.CodigoInterno=c.Codigo);
    IF NOT EXISTS(SELECT 1 FROM dbo.SOLICITUDES_CLIENTE WHERE IdCliente=@Cliente AND IdTipoOperacion=1 AND Descripcion=@Descripcion)
        INSERT dbo.SOLICITUDES_CLIENTE(IdCliente,IdTipoOperacion,IdEstadoOperacion,FechaSolicitud,Descripcion,
            CantidadBanos,IdTipoBanoSolicitado,TipoUsoSolicitado,DireccionSolicitada,FechaDeseada)
        VALUES(@Cliente,1,1,@Ahora,@Descripcion,2,@Tipo,N'Evento',N'PRUEBA LOCAL - Entrega de servicio nuevo 789',@Ahora);
    COMMIT;
    SELECT IdSolicitud,CantidadBanos,TipoUsoSolicitado,DireccionSolicitada,IdEstadoOperacion
    FROM dbo.SOLICITUDES_CLIENTE WHERE IdCliente=@Cliente AND IdTipoOperacion=1 AND Descripcion=@Descripcion;
    SELECT b.IdBano,b.CodigoInterno,b.IdEstadoBano FROM dbo.BANOS_QUIMICOS b JOIN @Codigos c ON c.Codigo=b.CodigoInterno;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
-- Repetir reutiliza estos registros. No restablece estados ni libera unidades ya asignadas.
