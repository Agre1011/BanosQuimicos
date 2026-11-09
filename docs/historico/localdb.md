> Documento histórico archivado el 08/11/2026. Conserva decisiones, resultados y pendientes de su etapa; no es una guía vigente. Las actualizaciones posteriores pueden sustituir sus reglas. Consultar el [plan actual](../plan-trabajo.md) y el [índice histórico](indice.md).

# LocalDB: diagnóstico y recuperación sin recrear la instancia

Estado final comprobado: `(localdb)\MSSQLLocalDB` funciona con BanosQuimicosDb y la configuración normal de desarrollo. No hay una dirección temporal de canal en la configuración de la API ni en las pruebas.

## Qué ocurría

Se observaron simultáneamente:

- `SqlLocalDB info MSSQLLocalDB` indicaba Stopped y no devolvía un canal.
- El registro del usuario apuntaba al nombre interno LOCALDB#7AB46D6F y al directorio de MSSQLLocalDB.
- Existía un proceso SQL Server 2019, PID 19936, ejecutándose como LOCALDB#F0CB2303 sobre los mismos master.mdf/mastlog.ldf y el mismo directorio de la instancia.
- Ese motor sí respondía por su canal local y tenía BanosQuimicosDb y las dos bases de pruebas existentes online.

Diagnóstico comprobado: el proceso que mantenía abierta la instancia y el estado/nombre que conocía el administrador de LocalDB estaban desincronizados. Por eso el acceso directo llegaba al motor, mientras la resolución por `(localdb)\MSSQLLocalDB` intentaba iniciar una instancia que consideraba detenida y fallaba. No se determinó qué evento original produjo la desincronización; no se atribuye sin evidencia a corrupción de datos, a las entidades ni a la coexistencia de versiones 2019/2025.

Dentro del aislamiento de herramientas hubo además errores de acceso al registro. Para diagnosticar LocalDB se usaron consultas autorizadas fuera de ese aislamiento, con el mismo usuario de Windows propietario de la instancia.

## Recuperación realizada

1. Se verificaron identidad del servidor, proceso y archivos de la instancia. Se anotaron nombres, database_id y create_date de las bases para comparar después.
2. Se comprobaron sesiones y transacciones: no había transacciones de usuario abiertas; solo una conexión ociosa de Visual Studio. La parada normal desconectó esa conexión, sin descartar cambios pendientes.
3. En el canal del motor identificado se ejecutó `SHUTDOWN` normal, **sin** `WITH NOWAIT`. SQL Server realiza checkpoints antes de finalizar.
4. Se confirmó que ese proceso terminó y se ejecutó `SqlLocalDB start MSSQLLocalDB`.
5. `SqlLocalDB info MSSQLLocalDB` pasó a Running. `sqlcmd` y la API conectaron por el nombre normal, sin canal ni variables que sustituyeran la conexión.

No se usó `delete`, `create`, `-k`, `taskkill`, borrado de MDF/LDF, edición del registro, cambio de versión ni instalación/reparación de componentes.

Las tres bases anteriores conservaron database_id y create_date. BanosQuimicosDb conserva su id 8, 13 tablas (12 del negocio más historial EF), la migración 20261027203511_Inicial y 0 usuarios/0 clientes. La repetición de pruebas por nombre normal agregó otra base aislada identificada, sin borrar las previas.

Referencias: [SHUTDOWN normal y checkpoints](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/shutdown-transact-sql?view=sql-server-ver17), [utilidad SqlLocalDB](https://learn.microsoft.com/en-us/sql/tools/sqllocaldb-utility?view=sql-server-ver17).

## Comprobación normal reproducible

Desde una terminal de Windows del usuario propietario de LocalDB, sin cambiar conexiones:

```powershell
SqlLocalDB info MSSQLLocalDB
SqlLocalDB start MSSQLLocalDB
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d BanosQuimicosDb -b -l 15 -Q "SELECT DB_NAME() AS BaseActual; SELECT COUNT(*) AS Roles FROM dbo.ROLES;"
```

Se espera Running, BaseActual = BanosQuimicosDb y Roles = 3. Un arranque normal con la instancia ya ejecutándose no debe recrearla.

Con el servidor web detenido y sin operaciones pendientes, una parada/reanudación normal puede hacerse con `SqlLocalDB stop MSSQLLocalDB` seguido de `SqlLocalDB start MSSQLLocalDB`. No utilizar `-i` ni `-k` como reparación rutinaria.

Si se repite exactamente la inconsistencia (Stopped pero un motor mantiene abiertos esos archivos), `stop` por nombre puede no encontrar ese proceso. Antes de intervenir:

1. Cerrar la API y ventanas de datos que estén ejecutando operaciones. No cerrar herramientas con cambios sin guardar.
2. Revisar `%LOCALAPPDATA%\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\error.log` y la línea de comando del proceso. Identificar el canal **actual** anunciado por ese motor, su usuario y archivos; no copiar canales de esta sesión.
3. Conectar temporalmente a ese canal desde SSMS/sqlcmd solo para administración; confirmar `@@SERVERNAME`, bases, sesiones y transacciones. Si no se puede confirmar la identidad o hay trabajo activo, detenerse antes de interrumpirlo.
4. Ejecutar `SHUTDOWN` normal en ese motor, esperar su finalización y volver al arranque por nombre indicado arriba. La conexión se corta por el apagado; no agregar `WITH NOWAIT` para evitar esa desconexión esperada.
5. Repetir las consultas por `(localdb)\MSSQLLocalDB`. No dejar el canal como conexión de la aplicación, alias ni variable permanente.

Este procedimiento excepcional no es un script automático y no debe aplicarse a otros procesos SQL Server del equipo. Si la parada normal no basta, recopilar el diagnóstico antes de reparar componentes o cambiar archivos. No recrear la instancia/base como primer recurso.

## Comprobación de la API y las pruebas

Se comprobó que no había ConnectionStrings__BanosQuimicos en el entorno ni una conexión alternativa en User Secrets (solo la clave JWT). La configuración de appsettings.Development.json permaneció intacta.

Se arrancó la API con:

```powershell
dotnet run --project BanosQuimicos.Api --no-build --launch-profile http
```

`GET /api/estado` respondió 200. Para probar una consulta real de EF, se envió un login con un correo de diagnóstico inexistente: respondió 401 con «Correo o contraseña incorrectos.» y se observó en el log la consulta SELECT de USUARIOS/ROLES, sin error SQL. No se registraron usuarios en la base principal.

Se repitió `dotnet test BanosQuimicos.Api.Pruebas` usando exclusivamente `(localdb)\MSSQLLocalDB`: **12 pruebas correctas**. Se retiró la variable alternativa de servidor de la infraestructura de pruebas. La base nueva conservada es BanosQuimicosAuthPruebas_f319f196af514cbe87260a3a7473d0e4.
