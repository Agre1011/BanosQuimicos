# LocalDB: comprobación y recuperación

Conexión normal: `(localdb)\MSSQLLocalDB`, base `BanosQuimicosDb`, autenticación Windows del propietario. No dejar un canal temporal como conexión permanente. El [incidente original](historico/localdb.md) conserva diagnóstico, recuperación y pruebas de aquel momento; sus PID, nombres internos y conteos no describen el estado actual.

## Comprobación normal

```powershell
SqlLocalDB info MSSQLLocalDB
SqlLocalDB start MSSQLLocalDB
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d BanosQuimicosDb -b -l 15 -Q "SELECT DB_NAME() AS BaseActual; SELECT COUNT(*) AS Roles FROM dbo.ROLES;"
```

Esperado: Running, BanosQuimicosDb y 3 roles. No ejecutado de nuevo como parte de limpieza. Si falla, revisar mensajes/log antes de tocar datos o configuración. API `/api/estado` solo prueba respuesta HTTP; una consulta autenticada válida también necesita SQL.

## Inconsistencia excepcional

Se observó anteriormente Stopped mientras un proceso conservaba abiertos los archivos de la instancia; el motor respondía por canal, pero no por nombre. No se determinó el evento inicial ni se demostró corrupción. No usar el PID/canal de ese incidente hoy.

1. Detener ordenadamente API y operaciones de herramientas, conservando cambios sin guardar. Revisar `%LOCALAPPDATA%\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\error.log` y proceso propietario/archivos.
2. Solo si se confirma exactamente ese caso, identificar canal actual temporalmente para diagnóstico. Confirmar servidor/bases/sesiones/transacciones antes de intervenir; si hay trabajo activo o identidad dudosa, detenerse.
3. Con identidad confirmada y sin trabajo activo, una parada SQL normal `SHUTDOWN` (sin WITH NOWAIT) permite checkpoints. Esperar finalización y arrancar por nombre. Una parada/reanudación normal por nombre con herramientas cerradas usa `SqlLocalDB stop MSSQLLocalDB` y `start`.
4. Volver a consultar por nombre normal. Nunca fijar el canal como solución de la API ni como alias permanente.

No usar delete/create, `-k`, `-i`, taskkill, borrado de MDF/LDF, cambios de registro/versión ni reparaciones destructivas como rutina. No aplicar este procedimiento a otro motor. Si no basta, recopilar diagnóstico y consultar antes de reparar.
