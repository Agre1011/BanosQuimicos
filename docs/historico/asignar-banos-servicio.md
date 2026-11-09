> Documento histórico archivado el 08/11/2026. Conserva decisiones, resultados y pendientes de su etapa; no es una guía vigente. Las actualizaciones posteriores pueden sustituir sus reglas. Consultar el [plan actual](../plan-trabajo.md) y el [índice histórico](indice.md).

# Servicio nuevo: selección de baños y entregas

## Comportamiento confirmado

Desde Solicitudes y Reclamos se abre el pedido nuevo y se muestra la información registrada por el cliente: cantidad, tipo solicitado, uso, dirección, fecha deseada y descripción, además de su identidad y estado. En ese mismo detalle se agrega la selección autorizada, basada en Resolver solicitudes y reclamos, conservando presentación y navegación existentes.

Se confirmaron una **frecuencia de limpieza común**, indicada por el administrador (0 cuando no corresponde), y **un empleado activo y una fecha comunes para todas las entregas**. La frecuencia es referencial: no genera limpiezas automáticas. La fecha deseada del cliente se muestra para consultar; administración elige la fecha programada.

Al confirmar se selecciona exactamente la cantidad solicitada, sin repetir IDs. Cada unidad debe ser Apto, del tipo físico solicitado, sin servicio Pendiente de entrega/Entregado y sin mantenimiento bloqueante Pendiente/En curso. El tipo físico y el uso son conceptos separados: una unidad tipo Obra puede atender un uso Evento si ese es el tipo pedido. No se sustituye silenciosamente el tipo solicitado por otro.

La confirmación guarda, en una sola transacción:

- Solicitud Aprobada, sin FechaResolucion.
- Un servicio por baño, con cliente y solicitud de origen, dirección solicitada, uso, frecuencia común y fecha de asignación/programación. Estado Pendiente de entrega y FechaEntregaReal nula.
- Una ubicación preparada por servicio, todavía sin vigencia efectiva.
- Una tarea Entrega/Pendiente por servicio, vinculada al baño, solicitud, ubicación y empleado elegidos. Sin observaciones ni reposición de papel inventadas.

Los baños quedan asignados desde esa confirmación; no se reservan para otro pedido mientras estén pendientes de entrega. La selección no cambia al responsable habitual de limpieza. Si una unidad dejó de estar disponible o el empleado no está activo, no se guarda ninguna parte del pedido.

El empleado usa el recorrido existente de tareas. Registrar Trabajo guarda el cumplimiento sin entregar aún el servicio; Actualizar Estado finaliza la entrega, establece FechaEntregaReal y hace vigente su ubicación preparada con FechaDesde real. El primer baño entregado no resuelve un pedido de varios: se requiere completar todas las entregas, cantidades y trabajos necesarios. Tareas canceladas no se ignoran para resolver. Las referencias históricas se conservan.

## Persistencia y reintentos

No hay migración nueva: se reutilizan las 12 tablas, las relaciones solicitud/servicios/ubicaciones/tareas y el índice exclusivo de baño asignado. La primera entrega del lote conserva la ClaveProgramacion enviada; las restantes reciben claves propias. Reenviar la misma operación recupera el conjunto persistido sin duplicarlo, incluso después de completar las entregas. Otra clave sobre un pedido ya vinculado no crea servicios adicionales. Una clave reutilizada con datos distintos devuelve 409.

No se reparan ni sobrescriben automáticamente conjuntos históricos parciales o inconsistentes. Si existen servicios/trabajos previos sin corresponder al reintento reconocido, se muestra el estado existente y se requiere revisión. No se implementan reemplazos, cancelación, borrados ni selección parcial de la cantidad. Se descartó el adicional vinculado: para otro baño se usa el pedido de servicio nuevo existente, pudiendo repetir dirección. Los servicios resultantes son independientes; no hay agrupación ni herencia de datos. Los tipos y registros históricos se conservan sin ofrecer un recorrido nuevo para ellos.

Las escrituras comparten transacción Serializable y bloqueo SQL con asignación/cumplimiento. La API revalida administrador, empleado y disponibilidad. El índice único filtrado existente evita dos asignaciones vigentes de una unidad. Todas las operaciones son exclusivas de Administrador/Activa; ocultar el selector no sustituye ese permiso.

## API

- GET /api/solicitudes/{id}/banos-disponibles: unidades disponibles del tipo solicitado para servicio nuevo.
- POST /api/solicitudes/{id}/asignar-banos: claveProgramacion (GUID), idBanos (lista), idEmpleado, idEstadoAnterior (1 o 2), cantidadLimpiezasSemana y fecha (YYYY-MM-DD). Devuelve 200 con detalle y servicios/entregas asociados, sin credenciales.
- 400 para cantidad distinta, IDs repetidos o datos inválidos; 404 solicitud inexistente; 409 cambio de estado, disponibilidad o contexto incompatible; 401/403 por autenticación/permiso.
- GET /api/solicitudes/{id} agrega servicios vinculados y el baño de cada tarea. Al reabrir muestra los estados persistidos, no los valores de la selección anterior.

## Datos preparados en LocalDB

Se ejecutó scripts/preparar-servicio-nuevo-local.sql solo en (localdb)\MSSQLLocalDB / BanosQuimicosDb, con guardas de base/IsLocalDB. Agregó:

- **Solicitud 4**, identificada por **PRUEBA LOCAL SERVICIO NUEVO - DOS BANOS**, Pendiente: cantidad 2, tipo Obra, uso Evento, dirección **PRUEBA LOCAL - Entrega de servicio nuevo 789**.
- **PRUEBA-NUEVO-01** (IdBano 2) y **PRUEBA-NUEVO-02** (IdBano 3), Apto y sin asignación. Sus responsables habituales no se inventaron ni se copiaron del empleado de entrega.

Se conservaron los 6 usuarios, el servicio anterior y las 4 tareas previas. No se crearon servicios ni entregas de esta solicitud: quedan para confirmar desde la app. El script reutiliza sus registros al repetir, sin liberar baños, cambiar estados o reabrir trabajos. Un primer intento se detuvo por diferencia de collation de la variable de códigos; se corrigió esa variable local para coincidir con CodigoInterno, sin cambiar la collation de la base.

## Prueba corta en Windows

1. Detener la API anterior en Visual Studio, compilar y reiniciarla con perfil https. Verificar LocalDB/BanosQuimicosDb y UsarHosting=false. Windows usa https://localhost:7292/. No ejecutar contra MonsterASP. Se mantiene la configuración local/USB/HTTPS de los bloques previos.
2. Ingresar como administrador → Solicitudes y Reclamos. Abrir el servicio nuevo más reciente y verificar la descripción **PRUEBA LOCAL SERVICIO NUEVO - DOS BANOS** (solicitud 4). Existe otro ejemplo anterior de servicio nuevo: comprobar la descripción antes de asignar para no consumir las unidades con otro pedido.
3. Verificar cantidad 2, tipo Obra, uso Evento y dirección. Seleccionar PRUEBA-NUEVO-01 y PRUEBA-NUEVO-02, empleado **PRUEBA CUENTAS Windows**, una fecha y frecuencia común (por ejemplo, 0 para esta prueba de uso Evento). Seleccionar no guarda. Cancelar permite empezar de nuevo sin cambios.
4. Confirmar **Aprobar y asignar baños y entregas**. Debe aparecer solicitud Aprobada, dos servicios Pendientes de entrega y dos tareas Entrega/Pendiente, cada una con su código de baño. Al volver y abrir nuevamente se conservan. No se deben crear otros al reintentar/reabrir. Los baños ya no están disponibles para otro servicio, aunque aún no se entregaron.
5. Cerrar sesión e ingresar como el empleado. En Tareas asignadas, abrir la primera entrega y completar Registrar Trabajo → Actualizar Estado. El servicio correspondiente queda Entregado; la solicitud continúa Aprobada mientras falte la segunda. El contador de hoy solo incluye estas tareas si se eligió hoy como fecha.
6. Completar la segunda entrega. Como administrador, consultar nuevamente el pedido: dos servicios Entregados y solicitud Resuelta con fecha. En Estado Baños se debe ver la dirección ya vigente del servicio. Conservar resultados como historial; no restablecer tareas o unidades para repetir la prueba.

La comprobación visual y de navegación de esta adaptación queda a cargo de la prueba manual Windows. La integral Android será al terminar los bloques, contra API local; solo después se planifican migraciones/republicación del hosting. Se mantienen pendientes vencimiento real del token y alternancia de servidores en dispositivo.

## Verificaciones ejecutadas

Se agregaron **9 casos automatizados**, sin nuevas pruebas para cambios visuales simples. Se ejecutaron 9 focalizados durante el desarrollo y **una única suite completa de cierre de 134 casos** (125 existentes + 9 nuevos), todos correctos: **143 ejecuciones de casos en total**. Cubren cantidad/tipo/condición/disponibilidad, dos pedidos simultáneos por la misma unidad con reversión completa del perdedor, reintentos, permisos, empleado inactivo, datos obligatorios, consistencia servicio/ubicación/entrega y resolución solo tras ambas entregas. El caso de tipo Obra con uso Evento verifica que no se confundan esos conceptos. Los datos automatizados están en bases LocalDB aisladas y se conservan.

API/pruebas compilaron en tmp/pruebas-asignar-banos. App Debug Windows y Android compilaron sin errores ni advertencias, usando los parámetros temporales de memoria Java ya documentados. La prueba manual Windows fue confirmada posteriormente según el cierre siguiente. No hay cambios en hosting, commit, push o merge.

## Cierre manual confirmado

**Windows aprobado por el estudiante:** en la solicitud **4**, asignó los dos baños, completó una entrega y comprobó que la solicitud seguía **Aprobada**; al completar la segunda, comprobó el cambio a **Resuelta**.

Esta prueba manual es independiente de las pruebas automatizadas anteriores (9 nuevas; suite completa de 134 casos) y de las compilaciones. No se repitieron por este cierre exclusivamente documental. Los estados iniciales de los datos de ejemplo describen su preparación; se conservan las asignaciones y entregas resultantes, sin restablecerlos para repetir la prueba.

**Bloque cerrado y listo para commit.** La integral Android contra API local queda para el final de los bloques; las migraciones/republicación del hosting serán posteriores. Siguen pendientes vencimiento real del token y alternancia entre servidores. Sin migración nueva en este bloque, cambios en hosting, commit, push ni merge.
