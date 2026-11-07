# Solicitudes y Reclamos: administrador → empleado

## Alcance y decisiones confirmadas

Se habilita Inicio del administrador → Solicitudes y Reclamos → Detalles → Volver, usando las vistas de página 6 del PDF aprobado. Listado y detalle consultan datos reales y contemplan carga, vacío y error. El detalle muestra los datos existentes, descripción y trabajos vinculados; no inventa un baño en una solicitud nueva ni reemplaza información ausente con ejemplos de Figma.

El estudiante autorizó expresamente, en el mismo detalle, elegir **Limpieza extra o Mantenimiento** para un reclamo y, en mantenimiento, indicar **si bloquea nuevas asignaciones**. También confirmó **Aprobar y asignar** en una única confirmación: solicitud Aprobada y tarea Pendiente, guardadas juntas. Se reutiliza el selector de fecha para la programación manual; la fecha deseada del cliente se muestra como referencia y no se convierte silenciosamente en programación.

Seleccionar empleado/tipo/fecha no guarda. El botón Aprobar y asignar (o Confirmar asignación si ya estaba Aprobada) realiza la escritura; Cancelar selección no modifica nada. Puede elegirse cualquier Empleado/Activa. **El responsable habitual del baño permanece intacto.**

## Tratamiento por tipo

- **Limpieza extra:** tarea Limpieza extra vinculada al servicio y solicitud.
- **Reclamo con trabajo:** el administrador elige Limpieza extra o Mantenimiento. Solo mantenimiento admite el indicador bloqueante. Una descripción sobre papel no implica que se haya repuesto: ese campo permanece sin completar.
- **Baja:** tarea Retiro. Asignarla no libera el baño; el cierre del retiro finaliza el servicio.
- **Cambio de dirección solicitado:** tarea Traslado, con origen actual y destino preparado desde NuevaDireccion. La dirección actual no cambia al asignar; el cierre activa el destino y conserva el historial anterior.
- **Traslado ya informado:** consulta con aviso de validación administrativa pendiente. No se envía otro traslado ni se valida retroactivamente desde esta asignación.
- **Servicio nuevo y baño adicional:** consulta de sus datos, sin asignación operativa desde esta pantalla. Requieren selección de unidades disponibles y creación de servicios en el bloque correspondiente. No se convierten en limpieza ni se aprueban/resuelven aquí.
- **Reclamo sin trabajo:** requiere respuesta administrativa conforme a la regla aprobada; no se crea una tarea ficticia. La captura/validación de esa respuesta no se implementa en este bloque de asignación.

No se implementa papelera ni DELETE: no hay regla de borrado aprobada. El icono del prototipo no autoriza eliminar historial. Tampoco se agregan rechazo/cancelación administrativa, sustitución de tareas canceladas ni reasignación de tareas existentes.

## Modelo y garantías

Sin entidades nuevas ni migración: se reutilizan las 12 tablas, IdSolicitud/IdEmpleado/FechaProgramada de TAREAS_OPERATIVAS y su ClaveProgramacion única, además de las ubicaciones de origen/destino existentes. Las migraciones locales pendientes de publicar de los bloques anteriores continúan pendientes; este bloque no añade otra.

GET /api/solicitudes y GET /api/solicitudes/{id}: solo Administrador/Activa, DTOs explícitos sin contraseñas, hashes o tokens, orden descendente por fecha/ID. El detalle incluye estados de los trabajos y resolución, para comprobar el avance al consultar nuevamente.

POST /api/solicitudes/{id}/asignar recibe claveProgramacion, idEmpleado, idEstadoAnterior (1 Pendiente o 2 Aprobada), idServicioEsperado, idUbicacionEsperada, idTipoTareaReclamo (3/11 solo en reclamos), bloqueaAsignacion y fecha (YYYY-MM-DD). Devuelve 200 con el detalle guardado. 400: datos inválidos/extra; 404: inexistente; 409: contexto/estado/asignación incompatible; 401/403: autenticación/permiso.

Se revalida administrador activo dentro de una transacción Serializable con el mismo bloqueo SQL de programación/cumplimiento. Empleado elegido debe estar activo; servicio Entregado del mismo cliente y ubicación actual deben coincidir con los mostrados. La tarea fija baño/servicio/ubicación/solicitud, sin editar el responsable habitual, observaciones o papel. No apto/mantenimiento no liberan automáticamente un servicio entregado ni impiden crear el trabajo necesario para atenderlo; un nuevo servicio sí deberá respetar disponibilidad.

Misma clave y mismos datos recuperan el trabajo anterior sin duplicarlo, incluso después de finalizarlo; otros datos o tarea cancelada producen 409. Otra clave sobre una solicitud con trabajos también produce 409. No se interpreta un reintento como una reasignación, otro trabajo o una sustitución. Los trabajos previos se conservan y se muestran para revisión administrativa. El primer trabajo creado aquí no impide que el modelo represente varios trabajos; este endpoint no agrega otros si ya hay historial que requiera revisión.

La solicitud solo se resuelve al completar los trabajos requeridos, según el cierre existente: aprobar/asignar/registrar cumplimiento no resuelven por sí solos. Tareas canceladas no se excluyen para aparentar cumplimiento. Retiro y traslado aplican sus efectos en la misma transacción del cierre. Los estados finales no se reabren. Cero tareas tampoco resuelve automáticamente una solicitud.

## Datos locales preparados

Se ejecutó scripts/preparar-solicitudes-local.sql exclusivamente en (localdb)\MSSQLLocalDB / BanosQuimicosDb. Tiene guardas IsLocalDB/nombre de base y requiere el servicio Entregado de PRUEBA-TAREAS-01. No crea usuarios, cambia contraseñas, modifica responsables, reabre tareas ni toca hosting. Repetirlo reutiliza los ejemplos sin restablecer su estado.

Ejemplos identificables preparados:

- Solicitud **1**: PRUEBA LOCAL SOLICITUDES - LIMPIEZA EXTRA.
- Solicitud **2**: PRUEBA LOCAL SOLICITUDES - RECLAMO.
- Solicitud **3**: PRUEBA LOCAL SOLICITUDES - NUEVO SERVICIO (consulta para el bloque futuro).

Los tres comenzaron Pendientes, sin tareas asociadas. Los dos primeros usan el cliente/servicio de prueba existente. No se preparó una baja sobre ese servicio, para que la prueba manual de consulta/limpieza no retire accidentalmente la unidad usada en otros bloques. Baja y traslado se prueban automáticamente en bases aisladas.

## Prueba breve en Windows

1. Detener la API anterior en Visual Studio, compilar y reiniciarla con perfil https. Verificar la conexión **LocalDB/BanosQuimicosDb**. La app mantiene UsarHosting=false: Windows usa https://localhost:7292/. No publicar ni cambiar la conexión a MonsterASP. Si se inicia por terminal, fijar la conexión local en esa terminal y ejecutar dotnet run --project BanosQuimicos.Api --launch-profile https. /api/estado no identifica la base por sí solo.
2. Entrar como administrador local → Solicitudes y Reclamos. Abrir el ejemplo **1**, comprobar Cliente Prueba, PRUEBA-TAREAS-01, descripción y Pendiente. Consultar el ejemplo **3**: debe explicar que requiere selección de baños y no permitir asignarlo como limpieza.
3. En el ejemplo 1, abrir Asignar ▾, elegir el empleado local **PRUEBA CUENTAS Windows** y la fecha a probar. Cancelar y verificar que sigue Pendiente/sin tareas. Volver a elegir y pulsar **Aprobar y asignar**. Esperado: confirmación, solicitud Aprobada, tarea Limpieza extra/Pendiente con empleado y fecha. El habitual del baño se conserva.
4. Cerrar sesión e ingresar como el empleado. Abrir Tareas asignadas → tarea vinculada a esa descripción. **Registrar Trabajo** guarda cumplimiento y conserva estado; **Actualizar Estado** finaliza. Volver al administrador, abrir la solicitud y Consultar nuevamente: debe mostrar **Resuelta**, fecha de resolución y trabajo Realizada. Conservar el resultado como historial.
5. Repetir con el ejemplo **2** eligiendo Mantenimiento y el indicador de bloqueo según se quiera probar, sin inventar reposición/observaciones. El bloqueo, si se indicó, deja de estar vigente al finalizar, pero no cambia la condición física del baño. Elegir un empleado distinto del habitual también es válido si se dispone de otro activo local; no crear ni documentar contraseñas para automatizar esta prueba.
6. Comprobar ventana reducida/desplazamiento, regreso, falta de conexión y consulta después de recuperarla. Cliente/Empleado no deben ver el acceso administrativo y sus peticiones reciben 403. Repetir la consulta después de una asignación ya guardada debe mostrar la tarea existente, no permitir duplicarla.

La prueba integral manual Android se pospone hasta terminar los bloques funcionales y se hará contra la API local (Android Debug por USB con adb reverse tcp:5148 tcp:5148). Solo después de esa validación se planificarán migraciones y republicación del hosting. Compilar Android no cuenta como esa prueba manual. Vencimiento real del token y alternancia entre servidores siguen pendientes.

## Verificaciones y cierre

**Pruebas manuales Windows aprobadas**, confirmadas por el estudiante:

- Limpieza extra: aprobar y asignar, registrar y finalizar como empleado, consultar solicitud Resuelta.
- Reclamo con mantenimiento: elegir mantenimiento y bloqueo, asignar empleado, verificar bloqueo del baño, completar trabajo y comprobar reclamo Resuelto y levantamiento del bloqueo.

Son evidencias manuales independientes de las **125 pruebas automatizadas anteriores**, que verifican también permisos, concurrencia y casos de rechazo. Las compilaciones Windows y Android ya registradas resultaron sin errores ni advertencias; no se ejecutaron nuevamente por esta actualización documental. No se marcan como probados manualmente otros escenarios sin confirmación.

**Bloque cerrado y listo para commit.** Se conservan las solicitudes y trabajos resultantes de la prueba como historial. Pendientes: integral manual Android local al terminar los bloques, posterior actualización del hosting, vencimiento real del token y alternancia entre servidores. Sin cambios en hosting, commit, push ni merge. No hay una migración nueva en este bloque.

Ampliación posterior: [Servicio nuevo](asignar-banos-servicio.md) incorpora su selección autorizada de baños y entregas. Las referencias anteriores a ese tipo solo en consulta describen el alcance del bloque previo. Baño adicional y los otros tratamientos separados no cambian con esa ampliación.
