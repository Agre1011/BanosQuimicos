# Registrar Trabajo y Actualizar Estado: prueba local

## Recorrido autorizado

Todo se realiza en el detalle de tarea existente de Figma, sin formularios, desplegables ni pantallas nuevas. La adaptación fue autorizada por el estudiante el 06/11/2026.

1. **Registrar Trabajo** guarda la fecha/hora del servidor y el usuario autenticado que declara haber realizado el trabajo. No modifica el estado de la tarea, observaciones ni papel higiénico. El detalle confirma el registro después de recibir el resultado persistido.
2. **Actualizar Estado** requiere ese registro, del mismo empleado que tiene asignada la tarea. Guarda el estado interno Tarea/Realizada y FechaRealizada. La app muestra **Realizado** en verde. No equivale a editar arbitrariamente estados.
3. Ambos datos se recuperan desde la API al regresar al detalle o iniciar otra sesión. Las fechas se presentan en horario argentino (UTC−3). FechaTrabajoRegistrado es la declaración guardada; FechaRealizada es el cierre confirmado en el segundo paso.

El detalle incluye tipo de tarea, origen y descripción/observaciones disponibles: solicitud directamente vinculada (o solicitud de origen del servicio cuando no hay vínculo directo), observaciones del servicio y observaciones ya registradas del empleado. El texto se muestra escapado, conservando saltos de línea. La reposición solo se muestra si ya tiene valor; no se infiere de una limpieza o un reclamo. El catálogo actual no contiene un tipo de tarea separado para reposición: una indicación de papel puede estar en la descripción del reclamo/solicitud, sin inventar un tipo ni un valor booleano.

## Persistencia y seguridad

- Migración aditiva **20261106204848_RegistroTrabajoTarea**: dos columnas opcionales en TAREAS_OPERATIVAS (FechaTrabajoRegistrado e IdEmpleadoRegistroTrabajo), FK a USUARIOS sin cascada, índice y CHECK que exige ambos datos juntos o ambos nulos. Continúan las 12 tablas del negocio y la tabla técnica de EF. Las tareas históricas quedan sin registro nuevo; no se completan fechas ficticias.
- Aplicada únicamente en LocalDB/BanosQuimicosDb. Antes y después se verificaron 6 usuarios y 1 tarea; después, 12 tablas de negocio y 27 filas de catálogos. La tarea 1 sigue Pendiente/sin registro para la prueba personal. No se tocó MonsterASP.
- POST /api/tareas/{id}/registrar-trabajo y POST /api/tareas/{id}/finalizar, sin cuerpo. Solo Empleado/Activa, con propiedad verificada en SQL y cuenta revalidada dentro de la transacción. Identidad y fecha no se aceptan desde la app. Responden 200 con el detalle persistido; 404 para ajena/inexistente, 409 para estado o contexto incompatible. Se conservan 401/403 de autenticación/autorización.
- Pendiente o En curso admiten el primer paso sin transición de estado. Cancelada rechaza ambos. Realizada con registro del mismo empleado responde de forma idempotente; una histórica sin registro rechaza modificaciones. Registrar o finalizar repetidamente no reemplaza autor/fechas ni repite efectos.
- Si se reasigna antes de registrar, solo el nuevo empleado puede hacerlo. Si ya había un registro de otro empleado, no se sobrescribe y se requiere revisión administrativa: el anterior pierde acceso y el nuevo no puede finalizar apropiándose del cumplimiento. No se implementa reasignación administrativa en este bloque.
- Transacciones Serializable, bloqueo de actualización de la tarea y bloqueo de aplicación SQL compartido por este recorrido ordenan sus escrituras, incluidas tareas distintas de una misma solicitud. Es una decisión sencilla para este volumen: serializa estos dos endpoints en cada base; no pretende sustituir validaciones de futuros endpoints administrativos. Se libera al confirmar o revertir. Errores no dejan cierres parciales.
- Entrega confirma entrega del servicio; retiro cierra el servicio y libera su asignación; traslado activa el destino preparado y cierra el intervalo anterior, sin cambiar la ubicación de trabajos históricos. Se validan baño, servicio y ubicaciones. Limpieza/mantenimiento no convierten automáticamente la condición a Apto ni completan papel/observaciones. Al pasar a Realizada deja de contar un bloqueo de mantenimiento por estado; la condición física sigue siendo independiente.
- Una solicitud directamente vinculada solo se resuelve desde Aprobada y cuando todas sus tareas están realizadas. Las canceladas no se descartan. Nuevo servicio/baño adicional exigen además todos los servicios solicitados entregados y sus entregas realizadas; baja exige servicio finalizado y cambio de dirección su fecha efectiva. Cero tareas no resuelve nada; reclamos sin tareas siguen siendo responsabilidad administrativa. No se identifica automáticamente una sustitución de tarea cancelada: se conserva el caso sin resolver para revisión administrativa, según el límite del esquema actual.

## Prueba personal en Windows

1. Detener la API anterior en Visual Studio, compilar y volver a iniciarla para cargar estos endpoints. Mantener el destino local en BanosQuimicos.App/Plataforma/ConexionApi.cs: UsarHosting=false. La app Windows debe usar https://localhost:7292/; Android Debug http://127.0.0.1:5148/ con adb reverse. Se conserva la validación HTTPS de Windows y la excepción HTTP limitada a Android Debug/loopback.
2. En una terminal PowerShell nueva, si hace falta reproducir la migración en otra copia local:

```powershell
$env:ConnectionStrings__BanosQuimicos = 'Server=(localdb)\MSSQLLocalDB;Database=BanosQuimicosDb;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet ef dbcontext info --project BanosQuimicos.Api -- --environment Development
dotnet ef database update --project BanosQuimicos.Api -- --environment Development
```

Comprobar antes BanosQuimicosDb/(localdb)\MSSQLLocalDB. En esta PC ya se aplicó la migración: no es necesario repetirla. Si se inicia por terminal, usar dotnet run --project BanosQuimicos.Api --launch-profile https desde esa misma terminal. /api/estado no identifica la base por sí solo.

3. Ingresar con la cuenta local **PRUEBA CUENTAS Windows**, usando su contraseña personal. Abrir Tareas asignadas → PRUEBA-TAREAS-01 → Ver detalle. La tarea 1 es Limpieza extra/Pendiente, cliente Cliente Prueba, dirección PRUEBA LOCAL - Av. de ejemplo 123, programada 06/11/2026. Si se prueba otro día seguirá en el listado, pero no sumará al contador de hoy.
4. Antes del registro, Actualizar Estado está deshabilitado. Pulsar Registrar Trabajo: debe aparecer autor y fecha/hora; el estado sigue Pendiente y se habilita Actualizar Estado. No se modifica papel ni se agregan observaciones de ejemplo.
5. Volver al listado y entrar otra vez; cerrar/reabrir la app o cerrar sesión e ingresar con las mismas credenciales. Debe recuperarse el registro y continuar habilitado Actualizar Estado.
6. Pulsar Actualizar Estado: muestra Realizado en verde y la fecha del cierre; ambos botones quedan deshabilitados. Reabrir para verificar persistencia. No se restablece la tarea al finalizar: conservarla como evidencia.
7. En Android repetir con el teléfono físico y adb reverse tcp:5148 tcp:5148. Comprobar desplazamiento, textos largos y regreso. La compilación no constituye prueba manual en el dispositivo.

Los permisos y reintentos también se pueden repetir con los endpoints anteriores en Postman: otro empleado debe recibir 404 y un Cliente/Administrador 403. No colocar tokens ni contraseñas en documentos versionados. Para nuevas tareas usar la preparación local existente, sin modificar una tarea ya finalizada ni escribir en hosting.

## Programación semanal: aclaración para el bloque administrativo

Antes se había confirmado creación/programación/asignación manual de cada tarea, con frecuencia del servicio solo como referencia y sin generación automática. La intención aclarada ahora es que **el administrador asigne baños a empleados y esas asignaciones generen sus tareas semanales**. Esta aclaración reemplaza esa intención anterior para el futuro bloque administrativo; no se implementa aquí ningún generador ni asignación recurrente. Antes de ese bloque habrá que concretar periodicidad/fechas, prevención de duplicados y cambios o fin de asignación, y contrastar su representación con el modelo. No se crean tablas, pantallas ni reglas de generación en este bloque de cumplimiento de tareas existentes.

Los resultados automatizados, de compilación y los pendientes manuales se registran en la sección 18 del plan. Vencimiento real del token y alternancia entre servidores en el dispositivo continúan pendientes.

## Resultado manual y cierre (06/11/2026)

El estudiante confirmó el recorrido indicado en Windows como **aprobado**. La prueba manual de Android sigue pendiente. Esta confirmación es independiente de las 85 pruebas automatizadas anteriores; no se repitieron por esta actualización documental. La tarea utilizada se conserva con el resultado de la prueba, sin volverla artificialmente a Pendiente.

La migración 20261106204848_RegistroTrabajoTarea ya aplicada en LocalDB agregó a TAREAS_OPERATIVAS FechaTrabajoRegistrado (datetimeoffset nullable) e IdEmpleadoRegistroTrabajo (int nullable), su índice y FK a USUARIOS sin cascada y un CHECK para mantener autor/fecha juntos. EF registró su aplicación en __EFMigrationsHistory. Se mantienen las 12 tablas y sus datos.

**Hosting pendiente para el futuro bloque de publicación:** revisar el historial de db77593 y aplicar la migración aditiva antes de utilizar la API nueva; no volver a ejecutar el script inicial en la base existente. No se contactó ni modificó MonsterASP en este cierre. Bloque listo para commit, sin commit/push ni publicación del agente.
