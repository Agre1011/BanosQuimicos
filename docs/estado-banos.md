# Estado Baños: responsable habitual y limpieza manual

## Decisiones confirmadas y alcance

Desde Inicio del administrador → Estado Baños se consulta cada baño y se abre **Asignar ▾** en su tarjeta. El selector contiene empleados activos. Guardar responsable habitual cambia únicamente el responsable del baño; programar una limpieza elige empleado y fecha para una tarea independiente. Al abrir se propone el habitual si sigue siendo Empleado/Activa; el administrador puede elegir otro sin cambiarlo. Las tareas existentes conservan su empleado y contexto.

La frecuencia del servicio es una referencia. **No hay generación semanal automática.** Esta decisión confirmada reemplaza la intención de generación por asignación registrada en el bloque anterior. Una solicitud o reclamo podrá asignarse puntualmente a cualquier empleado sin alterar el habitual; ese recorrido corresponde al siguiente bloque.

Se utilizan las tarjetas de Estado Baños de página 6 del PDF y las capturas aportadas. Se autorizó el selector de fecha en esta vista. La futura pantalla para resolver servicio nuevo con selección de baños también fue autorizada, basada en Resolver solicitudes y reclamos, pero no se implementa aquí. Estas autorizaciones expresas amplían las excepciones visuales anteriores para esos elementos concretos; no habilitan otras pantallas inventadas. Los PDF/PNG originales permanecen intactos.

## Modelo y migración

La asignación puntual y la fecha ya estaban en TAREAS_OPERATIVAS.IdEmpleado y FechaProgramada. El DER original no contenía un responsable habitual persistente. Se explicó antes de cambiar el modelo la migración aditiva **20261106222844_ResponsableLimpiezaYProgramacion**:

- BANOS_QUIMICOS.IdResponsableLimpieza, int nullable, FK a USUARIOS sin cascada e índice. Las unidades existentes quedan Sin asignar hasta que el administrador elija; no se deduce un responsable a partir de una tarea pasada. El campo representa el responsable actual; no es una tabla nueva de movimientos. El historial operativo sigue en cada tarea.
- TAREAS_OPERATIVAS.ClaveProgramacion, uniqueidentifier nullable, índice único filtrado por valor no nulo. Identifica el envío lógico de una programación. Las tareas anteriores quedan nulas y se conservan.

Aplicada exclusivamente en LocalDB/BanosQuimicosDb, después de verificar IsLocalDB=1 e historial. Se conservaron 6 usuarios, 1 baño y 1 tarea ya Realizada del bloque anterior; el responsable y la clave de esos registros quedaron nulos. Se mantienen las 12 tablas del negocio. No se crearon datos operativos de ejemplo ni se reabrió la tarea realizada.

MonsterASP/db77593 sigue sin cambios. Para una futura publicación habrá que revisar su historial y aplicar las migraciones pendientes de RegistroTrabajoTarea y ResponsableLimpiezaYProgramacion antes de desplegar esta versión. No repetir el script de creación inicial sobre la base existente ni copiar cuentas/datos de LocalDB.

## Reglas de operación

- Responsable: cualquier baño existente puede recibir un empleado activo como habitual, esté disponible, en servicio o No apto. Esto no entrega, reserva ni habilita el baño. Repetir el mismo responsable es inocuo; el responsable anterior enviado evita pisar un cambio concurrente de otra sesión.
- Limpieza: genera una tarea **Limpieza semanal/Pendiente**, con empleado activo, día elegido, servicio Entregado del mismo baño y ubicación actual. No crea servicios ficticios para baños sin servicio ni limpia una unidad todavía pendiente de entrega mediante un registro incompatible con el modelo. El mantenimiento sin servicio conserva su recorrido separado.
- La API calcula Disponible como Apto, sin asignación vigente y sin mantenimiento bloqueante Pendiente/En curso. Las limpiezas pendientes no bloquean disponibilidad. Servicio asignado, condición y mantenimiento se presentan por separado.
- No apto o un mantenimiento bloqueante impiden nuevas asignaciones de servicio; no convierten en inexistente un servicio ya entregado ni prohíben su limpieza. Se advierte esa situación al administrador y se conserva condición/asignación. Programar o finalizar limpieza no vuelve Apto al baño ni completa papel/observaciones. No se agregan restricciones de limpieza por tipo Obra/Evento ni se convierte la frecuencia en un límite automático.
- FechaProgramada conserva el día elegido en Argentina a las 00:00 UTC−3, como fecha de planificación, no como hora efectiva del trabajo. No se exige una hora que la pantalla no pide. La ubicación se fija desde la actual al programar y no se redirige posteriormente una tarea histórica. Servicio o ubicación cambiados desde la lectura provocan 409 y requieren recargar.
- POST repetido con la misma clave y datos devuelve el mismo IdTarea (200); la primera creación devuelve 201. Reutilizar la clave para otros datos da 409. Una nueva clave con exactamente el mismo baño, servicio, ubicación, empleado y fecha de limpieza no crea otra tarea mientras la anterior no esté cancelada: devuelve 409. Es la protección contra volver a enviar los mismos datos al navegar/reiniciar. Elegir otra fecha/empleado/contexto identifica otra programación. Las canceladas se conservan y permiten una nueva programación con otra clave.
- Transacciones Serializable y el mismo bloqueo SQL de aplicación que usa el cumplimiento ordenan programación/cierre. El índice único refuerza la clave en la base. Se revalida el administrador y el empleado dentro de la transacción. No se modifican tareas anteriores ni se reasigna una solicitud al cambiar el habitual.

## Contratos de API

Solo Administrador/Activa. Anónimo/token inválido: 401; Cliente/Empleado/Pendiente: 403. DTOs sin contraseñas, hashes ni tokens.

- GET /api/banos/estado: tarjetas con condición/disponibilidad/contexto y responsable.
- GET /api/banos/empleados-asignables: ID y nombre de empleados activos, ordenados por apellido/nombre/ID.
- PUT /api/banos/{id}/responsable-limpieza: cuerpo con idEmpleado e idResponsableAnterior (null si Sin asignar); 200 con tarjeta actual, 404 inexistente, 409 empleado inválido o cambio concurrente.
- POST /api/banos/{id}/limpiezas: claveProgramacion (GUID), idEmpleado, idServicio, idUbicacion, fecha (YYYY-MM-DD); devuelve idTarea. Datos incompletos/extra: 400; contexto incompatible: 409; baño inexistente: 404.

## Prueba manual administrador → empleado

1. Detener la API anterior en Visual Studio, compilar y reiniciar en Debug con perfil https. Verificar conexión local: (localdb)\MSSQLLocalDB / BanosQuimicosDb. Esta PC ya tiene la migración aplicada. Para otra copia local, con la API detenida:

```powershell
$env:ConnectionStrings__BanosQuimicos = 'Server=(localdb)\MSSQLLocalDB;Database=BanosQuimicosDb;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet ef dbcontext info --project BanosQuimicos.Api -- --environment Development
dotnet ef database update --project BanosQuimicos.Api -- --environment Development
```

La app conserva UsarHosting=false en Plataforma/ConexionApi.cs. Windows usa https://localhost:7292/ y Android Debug http://127.0.0.1:5148/ mediante adb reverse tcp:5148 tcp:5148. Si se inicia la API desde terminal, ejecutar dotnet run --project BanosQuimicos.Api --launch-profile https en la terminal con esa conexión. /api/estado por sí solo no identifica la base. No cambiar el destino a hosting para estas pruebas.

2. Ingresar como administrador local y abrir **Estado Baños**. La unidad existente **PRUEBA-TAREAS-01** tiene el servicio entregado del cliente Cliente Prueba y dirección PRUEBA LOCAL - Av. de ejemplo 123. La tarea 1 realizada del bloque anterior se conserva. Al inicio el responsable es Sin asignar.
3. Pulsar **Asignar ▾**, seleccionar **PRUEBA CUENTAS Windows** y Guardar responsable habitual. Esperar la confirmación y el nombre en la tarjeta. Volver al inicio y reabrir para comprobar persistencia y que se propone ese empleado.
4. Seleccionar hoy y **Programar limpieza**. Debe aparecer la confirmación y un nuevo IdTarea. No repetir esta acción cambiando parámetros mientras se espera la respuesta. Si falla la conexión, el reintento conserva la clave y los datos de la misma operación; antes de iniciar otra, consultar el estado. La misma programación exacta no debe duplicarse después de volver a abrir.
5. Cerrar sesión e ingresar como ese empleado con su contraseña personal. Debe ver la nueva tarea Limpieza semanal y sumar en el contador de hoy. Abrir detalle, Registrar Trabajo y Actualizar Estado; comprobar Realizado en verde, persistencia al reabrir y regreso. La tarea 1 sigue realizada independientemente.
6. Para probar la excepción puntual, disponer de un segundo empleado activo local: registrarlo como Cliente/Pendiente, activarlo y confirmar cambio de rol como en el bloque previo. En Estado Baños elegirlo para otra fecha de limpieza **sin pulsar Guardar responsable habitual**. El habitual original debe conservarse y solo el segundo empleado debe recibir esa tarea. No registrar credenciales en documentos.
7. Probar cancelación de apertura del selector, fecha vacía (no permite programar), desplazamiento, volver y falta de conexión/recuperación. Otro usuario Cliente/Empleado no debe ver Estado Baños; sus llamadas administrativas reciben 403. En Android repetir en el Moto G9 Plus por USB. Compilación y renderer automatizado no equivalen a esta prueba nativa.

## Servicio nuevo: recorrido documentado para el bloque posterior

El cliente completa la solicitud con los campos acordados y existentes: cantidad y un tipo de baño, uso, dirección necesaria para prestar el servicio, fecha deseada/descripción cuando correspondan; su identidad se toma de la sesión. No es necesario que tenga dirección principal. La solicitud nace Pendiente y llega a **Solicitudes y Reclamos**.

El administrador abre el detalle y la pantalla autorizada para resolver servicio nuevo, basada en **Resolver solicitudes y reclamos**, con selección de baños disponibles. Debe comprobar Apto, sin asignación vigente y sin mantenimiento bloqueante; respetar cantidad/tipo y crear un servicio por baño, con ubicación y relación a solicitud origen. Aprobar no equivale a resolver; las entregas necesarias y sus empleados puntuales se programan/registran sin cambiar responsables habituales. Solicitud de baño adicional conserva servicio de referencia y genera otro servicio. La resolución requiere todos los trabajos y cantidades correspondientes, conservando historial.

Este documento no implementa solicitudes nuevas, selección/alta de servicios, entregas administrativas, baja, mantenimiento, edición de condición ni reasignación de tareas existentes. Los controles de esos módulos permanecen deshabilitados; no se crean pantallas fuera de lo autorizado. El futuro bloque concretará su interacción con esa referencia visual antes de implementarla.

Los resultados de pruebas y compilación se registran en la sección 19 del plan. La prueba manual Windows fue aprobada según el cierre siguiente; siguen pendientes Android, las migraciones/publicación en hosting, el vencimiento real del token y alternar servidores en el dispositivo.

## Cierre manual confirmado

**Windows aprobado por el estudiante:** guardar responsable habitual, programar una limpieza para el **07/11/2026**, ingresar como empleado, consultar la tarea, registrar el trabajo y finalizarla. Esta confirmación manual es independiente de las 100 pruebas automatizadas y compilaciones anteriores; no se repitieron por este cambio documental. La tarea y el responsable utilizados se conservan con el resultado de la prueba; las referencias anteriores a Sin asignar describen la preparación inicial.

**Android pendiente.** También queda pendiente aplicar al hosting, tras revisar su historial, la migración local 20261106222844_ResponsableLimpiezaYProgramacion: responsable opcional del baño con FK/índice y clave opcional de programación con índice único filtrado. Se mantiene pendiente RegistroTrabajoTarea si todavía no está aplicada allí. No se ejecutó nada en MonsterASP ni se repite el script inicial. Bloque cerrado y listo para commit, sin commit/push/publicación del agente.
