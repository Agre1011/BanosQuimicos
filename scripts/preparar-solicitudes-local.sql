-- Datos identificables de este bloque. Ejecutar como un único lote, SOLO LocalDB.
-- Reutiliza el servicio de prueba sin modificar usuario, contraseña, baño ni tareas anteriores.
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'BanosQuimicosDb' OR ISNULL(CONVERT(int,SERVERPROPERTY('IsLocalDB')),0) <> 1
    THROW 51000, 'Solo se permite BanosQuimicosDb en LocalDB. No ejecutar en hosting.', 1;
DECLARE @Servicio int, @Cliente int, @TipoBano int;
SELECT @Servicio=s.IdServicio, @Cliente=s.IdCliente, @TipoBano=b.IdTipoBano
FROM dbo.SERVICIOS s JOIN dbo.BANOS_QUIMICOS b ON b.IdBano=s.IdBano
WHERE b.CodigoInterno=N'PRUEBA-TAREAS-01' AND b.Observaciones=N'PRUEBA CONSULTA TAREAS - SOLO LOCAL'
  AND s.EstadoServicio=N'Entregado';
IF @Servicio IS NULL
    THROW 51000, 'Falta el servicio entregado de PRUEBA-TAREAS-01. No se crean cuentas ni servicios ficticios.', 1;
DECLARE @Ahora datetimeoffset=SYSDATETIMEOFFSET();
DECLARE @Hoy date=CONVERT(date,SWITCHOFFSET(@Ahora,'-03:00'));
DECLARE @Deseada datetimeoffset=TODATETIMEOFFSET(CONVERT(datetime2,@Hoy),'-03:00');
DECLARE @Ejemplos TABLE (Tipo int, Descripcion nvarchar(2000));
INSERT @Ejemplos VALUES
 (3,N'PRUEBA LOCAL SOLICITUDES - LIMPIEZA EXTRA: solicitar una limpieza adicional del baño de prueba.'),
 (5,N'PRUEBA LOCAL SOLICITUDES - RECLAMO: revisar el baño; el administrador debe elegir limpieza extra o mantenimiento. No afirmar reposición de papel sin registrarla.'),
 (1,N'PRUEBA LOCAL SOLICITUDES - NUEVO SERVICIO: ejemplo de consulta; requiere seleccionar baños en su bloque posterior.');
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT dbo.SOLICITUDES_CLIENTE
      (IdCliente,IdServicioReferencia,IdTipoOperacion,IdEstadoOperacion,FechaSolicitud,Descripcion,
       CantidadBanos,IdTipoBanoSolicitado,TipoUsoSolicitado,DireccionSolicitada,FechaDeseada)
    SELECT @Cliente,CASE WHEN e.Tipo=1 THEN NULL ELSE @Servicio END,e.Tipo,1,@Ahora,e.Descripcion,
       CASE WHEN e.Tipo=1 THEN 2 END,CASE WHEN e.Tipo=1 THEN @TipoBano END,
       CASE WHEN e.Tipo=1 THEN N'Obra' END,CASE WHEN e.Tipo=1 THEN N'PRUEBA LOCAL - Dirección de servicio nuevo 456' END,@Deseada
    FROM @Ejemplos e
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SOLICITUDES_CLIENTE s WHERE s.IdCliente=@Cliente AND s.IdTipoOperacion=e.Tipo
       AND s.Descripcion=e.Descripcion AND ISNULL(s.IdServicioReferencia,0)=CASE WHEN e.Tipo=1 THEN 0 ELSE @Servicio END);
    COMMIT;
    SELECT s.IdSolicitud,t.NombreOperacion,s.Descripcion,e.NombreEstado,s.IdServicioReferencia
    FROM dbo.SOLICITUDES_CLIENTE s JOIN @Ejemplos p ON p.Descripcion=s.Descripcion AND p.Tipo=s.IdTipoOperacion
    JOIN dbo.TIPOS_OPERACION t ON t.IdTipoOperacion=s.IdTipoOperacion
    JOIN dbo.ESTADOS_OPERACION e ON e.IdEstadoOperacion=s.IdEstadoOperacion
    WHERE s.IdCliente=@Cliente ORDER BY s.IdSolicitud;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
