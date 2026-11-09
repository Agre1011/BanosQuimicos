# Guía del administrador

Preparación común en [ejecución local](ejecucion-local.md); reglas y permisos en [alcance](alcance-y-reglas.md). Usar cuentas conocidas sin cambiar sus contraseñas y crear datos nuevos identificables para repetir pruebas. Estas instrucciones no afirman que todos sus pasos hayan sido probados manualmente: resultados en [verificaciones](verificaciones.md).

## Gestionar cuentas

Desde el inicio, abrir Gestionar cuentas: buscar por nombre/apellido/correo, filtrar estado/rol y consultar detalle. Los cambios requieren confirmación. Cambiar Cliente ↔ Empleado conserva la misma cuenta y no cambia su estado. La selección del desplegable **no guarda por sí sola**: confirmar el cambio y esperar el resultado.

Las operaciones de estado son independientes: activar antiguas Pendientes (Cliente/Empleado), suspender con motivo, reactivar Suspendidas o dar baja lógica definitiva desde la app. Si aparecen tareas, servicios o solicitudes bloqueantes, resolverlos por su recorrido antes de reintentar. No cancelar ni borrar por SQL para pasar la comprobación. No hay formulario de creación administrativa de empleados: se registran y luego se cambia su rol.

## Estado Baños

1. Agregar baño seleccionando tipo; guardar y anotar el código generado. Se crea Apto, sin servicio ni responsable. No editar códigos para fabricar resultados.
2. Asignar responsable habitual mediante Asignar ▾ y confirmar. Para limpieza, elegir fecha y empleado: se propone el habitual, pero puede ser otro. La frecuencia del servicio es referencia; cada limpieza se programa manualmente.
3. Cambiar condición Apto/No apto con confirmación. No apto no cancela trabajos ni cierra servicios. Para mantenimiento sin servicio, indicar descripción, fecha, empleado activo y bloqueo; el empleado lo cumple por su recorrido normal. Finalizar no cambia automáticamente a Apto.
4. Dar de baja del inventario solo sin servicio vigente ni tareas activas. Se conserva historial; consultar por filtro Bajas/Todos. La condición física se filtra con Todos/Aptos/No aptos, combinable con bajas. Actualizar conserva ambas selecciones.

Para comprobar mantenimiento: baño nuevo → No apto → mantenimiento bloqueante → intentar baja (debe identificar impedimento) → empleado finaliza → administración comprueba No apto y confirma Apto → baja de inventario → consultar mediante filtro. Un segundo mantenimiento bloqueante seguiría impidiendo disponibilidad aunque se marque Apto.

## Solicitudes y Reclamos

Abrir listado y detalle reales; identificar solicitud, cliente, servicio/baño, tipo y estado. No todos los pedidos se procesan igual:

- **Nuevo servicio:** revisar datos enviados, elegir cantidad exacta de baños del tipo solicitado y disponibles, frecuencia común, empleado y fecha común. Confirmar crea servicios Pendientes de entrega y una tarea por baño; solicitud Aprobada. Una entrega parcial no resuelve. Completar todas permite Resuelta.
- **Limpieza extra:** elegir empleado activo/fecha y confirmar Aprobar y asignar; tarea Pendiente vinculada. El empleado registra/finaliza; solicitud se resuelve cuando corresponde.
- **Reclamo con trabajo:** elegir Limpieza extra o Mantenimiento; solo mantenimiento permite bloqueo de nuevas asignaciones. No cambia responsable habitual.
- **Reclamo sin trabajo:** Responder y resolver, respuesta obligatoria, confirmar. Resuelta con respuesta/fecha, sin tarea ni cambios al servicio. Si hay tarea activa o registro sin finalizar se muestran bloqueantes. Reintento igual conserva respuesta/fecha; otra respuesta después del cierre produce conflicto.
- **Traslado solicitado:** elegir empleado/fecha. Revisar explícitamente qué otras tareas pendientes conservan origen o pasan a destino. En curso/con registro bloquean. La dirección no cambia hasta finalizar el traslado.
- **Traslado informado:** revisar fecha/hora efectiva y tareas; Validar aviso, explicación opcional. Cambia ubicación e historial y resuelve sin tarea física.
- **Baja:** atender mediante retiro en Baños y Servicios. No libera al solicitarla.

No hay borrado por papelera, rechazo/cancelación general de solicitudes ni edición de respuestas. El tipo histórico Baño adicional no se ofrece: usar nuevo servicio independiente.

## Baños y Servicios

Las tarjetas muestran baño/tipo/uso, cliente, dirección, frecuencia, estado y responsable habitual; no confundirlo con empleado de entrega/retiro. Filtro Todos/Baños de obra/Baños de evento usa el tipo físico, no el uso. Se conserva al actualizar/volver.

Baja permite seleccionar empleado/fecha y vincular una solicitud abierta existente; si hay varias, elegirla. Sin solicitud, se programa retiro administrativo sin inventar pedido del cliente. Antes de finalizar, revisar otros trabajos: cancelar explícitamente los Pendientes sin registro que no correspondan o decidir que deben completarse. No cancelar En curso/con registro ni descartar mantenimiento bloqueante. Confirmar revisión y comunicar al empleado que puede intentar cierre.

El retiro completado finaliza servicio y solicitud vinculada cuando corresponde; libera asignación, no necesariamente disponibilidad (condición/mantenimiento pueden impedirla).

## Recorrido de prueba entre roles

Grupo 1: cliente envía nuevo pedido → administración asigna dos baños y entregas → comprobar Aprobada. Grupo 2: empleado completa primera entrega (sigue Aprobada) y segunda (Resuelta) → cliente consulta servicios/contador. Grupo 3: programar limpieza o reclamo → empleado cumple → comprobar solicitud/baño actualizados. Grupo 4: baja → revisión explícita de pendientes → retiro → servicio Finalizado y disponibilidad según bloqueos.

Los scripts conservados en `scripts/preparar-*-local.sql` tienen escenarios identificables; revisar sus guardas y datos antes de ejecutarlos. No reinician trabajos ya finalizados. Los IDs de los ejemplos antiguos son evidencia histórica, no datos garantizados para una ejecución nueva.
