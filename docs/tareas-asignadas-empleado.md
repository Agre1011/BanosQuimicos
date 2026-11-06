# Tareas asignadas: guía local

Recorrido de Figma: **Inicio del empleado → Tareas asignadas → Ver detalle → Volver al listado → Volver al inicio**. Los valores de las capturas no se cargan como datos. Solo se implementa consulta.

## Criterios

El contador incluye las tareas propias con FechaProgramada dentro de hoy en Argentina (UTC−3), en cualquier estado; no es un contador de pendientes. Sin programación no suma. El listado muestra todas las tareas asignadas, también otras fechas, ordenadas por programación/IdTarea y con las no programadas al final.

La dirección proviene de la ubicación vinculada a la tarea, no de la ubicación actual del servicio. No se reescriben trabajos históricos al cambiar de dirección. Si falta fecha se muestra Sin programar; mantenimiento sin servicio puede indicar Sin servicio asociado y Sin dirección registrada.

## Preparación y recorrido manual

1. Detener la API local anterior antes de compilar y reiniciarla después: puede bloquear los binarios y seguir ejecutando la versión vieja. Compilar en Debug. Para Android con memoria limitada está documentada la alternativa Java en acceso-empleados.md.
2. En PowerShell nuevo, desde la raíz, seleccionar LocalDB explícitamente para esta terminal y sus procesos:

```powershell
$env:ConnectionStrings__BanosQuimicos = 'Server=(localdb)\MSSQLLocalDB;Database=BanosQuimicosDb;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet ef dbcontext info --project BanosQuimicos.Api --no-build -- --environment Development
dotnet run --project BanosQuimicos.Api --launch-profile https
```

Esperado: BanosQuimicosDb en (localdb)\MSSQLLocalDB; API localhost HTTPS 7292/HTTP 5148. La app mantiene UsarHosting=false. Observar BaseAddress en el depurador confirma el destino: Windows https://localhost:7292; Android Debug http://127.0.0.1:5148 por adb reverse. /api/estado 200 no identifica la base por sí solo.

3. Registrar desde la app local dos cuentas identificadas PRUEBA TAREAS: `prueba.tareas.empleado@example.invalid` y `prueba.tareas.cliente@example.invalid`. Contraseñas elegidas personalmente, sin guardarlas en documentos/chat. Activar ambas con el administrador local y cambiar la primera a Empleado, confirmando. La segunda sigue Cliente/Activa. No copiar cuentas del hosting.
4. Abrir **scripts/preparar-tarea-empleado-local.sql** desde Visual Studio/SSMS conectado a LocalDB/BanosQuimicosDb. Revisar los correos configurables y ejecutar como único lote. El script comprueba base local, cuentas activas y roles; no crea usuarios ni contraseñas, no borra datos y no modifica cuentas existentes. La preparación ya se ejecutó en LocalDB por autorización del estudiante; ver evidencia al final. Para otras cuentas, revisar los correos antes de ejecutar.
5. Esperado: una fila con IdTarea, IdEmpleado, código **PRUEBA-TAREAS-01** y programación hoy a las 10:00 de Argentina. Crea baño/servicio/ubicación/tarea de prueba; reutiliza su preparación al repetir el mismo día. Otro día puede crear una tarea nueva. No sobrescribe registros ajenos con ese código. Dirección ficticia identificable: **PRUEBA LOCAL - Av. de ejemplo 123**.
6. Ejecutar la app con Windows Machine e ingresar como el empleado. El contador incluye su tarea de hoy. Entrar al listado y Ver detalle de PRUEBA-TAREAS-01: Pendiente, Limpieza extra, cliente real de la cuenta de prueba, baño, dirección y fecha. Volver regresa al listado y después al inicio, que consulta otra vez el contador.
7. Crear otro empleado local sin asignaciones: debe obtener contador 0 y No tenés tareas asignadas. Con su token en Postman, el detalle del IdTarea del primero devuelve 404. Las consultas y escrituras por ID no le permiten acceder a tareas ajenas.
8. Repetir regreso, desconexión/reintento, ventana reducida y desplazamiento. Para Android Debug usar el Moto G9 Plus autorizado y `adb reverse tcp:5148 tcp:5148`; seleccionar el teléfono y F5 con API local abierta. La interfaz nueva requiere prueba manual de clics, teclado y rotación.

## Endpoints y seguridad

- GET /api/tareas/mias/resumen: fecha del día y cantidad.
- GET /api/tareas/mias: lista propia.
- GET /api/tareas/{id}: detalle propio; 404 para ajena o inexistente.

Solo Empleado/Activa. Anónimo/token inválido 401; Cliente, Administrador o Pendiente 403; Suspendida/BajaLogica con token previo 401. IdEmpleado proviene de la identidad actual validada, nunca de un parámetro del cliente. DTOs sin hashes, contraseñas ni contacto privado: IdTarea, Tipo, CodigoBano, Direccion, FechaProgramada, Estado y nombre Cliente.

No hay POST/PUT/PATCH/DELETE de tareas en este bloque: reciben 405 y no modifican datos. Al conectar las futuras acciones será obligatorio aplicar la misma propiedad por IdEmpleado antes de escribir. No se habilita eliminación por el icono de ejemplo en Figma.

## Única decisión pendiente para conectar acciones

El PDF permite marcar Realizada, agregar observaciones y registrar reposición de papel. El UML vincula registrar entrega/retiro/limpieza y mantenimiento con actualizar el estado de tarea mediante include; no detalla los formularios ni el reparto entre botones. No define **qué guarda y cómo confirma cada botón: Registrar Trabajo / Actualizar Estado**, ni muestra sus formularios. Se conservan visibles y deshabilitados, con aviso. Hace falta esa distribución; si abren otra pantalla, se necesita su captura aprobada antes de implementarla.

## Verificaciones

72 pruebas correctas (61 previas y 11 nuevas), con TestServer, cliente C# y SQL Server LocalDB en bases nuevas BanosQuimicosAuthPruebas_<GUID> que se conservan. Incluyen dos empleados, tareas sin asignar, detalle ajeno/inexistente, permisos, rechazo de escrituras y vacío; medianoche UTC frente a Argentina con reloj de prueba, fecha nula, ubicación histórica diferente de la actual y mantenimiento sin servicio. También se renderizan los Razor reales y se invocan callbacks para contador, listado, detalle y regreso: no sustituye interacción nativa.

La API abierta bloqueó la compilación normal; se usó salida aislada, sin detener ese proceso:

```powershell
dotnet test BanosQuimicos.Api.Pruebas --no-restore --output ./tmp/pruebas-tareas
```

Los primeros datos de prueba omitían FechaDesde de una ubicación actual y el CHECK los rechazó. Se corrigió la preparación sin debilitar la restricción; pasó la suite final. Script manual ejecutado posteriormente en LocalDB por autorización del estudiante; ver evidencia siguiente. Sin migraciones, publicación ni cambios en MonsterASP/db77593. Siguen pendientes el vencimiento real del token y alternar servidores en el dispositivo.

### Prueba manual confirmada y cierre (06/11/2026)

Windows: **aprobada por el estudiante**. Inicio → Tareas asignadas → Detalle → Volver al listado → Volver al inicio; datos coincidentes con la preparación local. Esta evidencia es manual y no sustituye ni amplía las pruebas automatizadas descritas arriba.

Preparación ejecutada solo en LocalDB/BanosQuimicosDb: tarea 1 para PRUEBA CUENTAS Windows (IdEmpleado 1004), baño PRUEBA-TAREAS-01, Limpieza extra/Pendiente, cliente Cliente Prueba, dirección PRUEBA LOCAL - Av. de ejemplo 123, fecha 06/11/2026 10:00 UTC−3. Se verificó la asignación con consulta SQL; no se modificaron cuentas ni contraseñas. Si se usa sqlcmd, agregar -I para habilitar QUOTED_IDENTIFIER, requerido por los índices filtrados.

Bloque de consulta cerrado. Pendientes: recorrido manual Android; definición e implementación de Registrar Trabajo y Actualizar Estado (ambos deshabilitados); vencimiento real del token y alternancia de servidores en el dispositivo. No se publicó ni se hizo commit/push.