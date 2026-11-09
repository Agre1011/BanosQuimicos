# Guía del cliente

Preparación: [ejecución local](ejecucion-local.md). Registro con campos de [alcance](alcance-y-reglas.md): crea Cliente/Activa y luego se inicia sesión; no espera aprobación. Validaciones en app/API, sin elegir rol/estado. Cuentas antiguas Pendientes conservan espera restringida.

## Inicio y consultas

Nombre real y contador de servicios vigentes: **Pendiente de entrega y Entregado**. Finalizado/Cancelado no cuentan. El resumen es total, no la cantidad de una página. Si falla consulta, mostrar error sin un cero engañoso.

Mis Servicios: tarjetas con código/tipo/uso, dirección y frecuencia; Ver detalle y Volver. Mis Solicitudes: tipo, fecha, descripción/resumen, estado y baño cuando corresponda; pedidos todavía sin asignación no inventan código. Reclamos respondidos muestran respuesta administrativa y fecha. Actualizar vuelve a consultar datos reales.

Ambos listados tienen diez elementos por página, total y controles; orden/filtro se aplican en API al conjunto completo. La sesión determina identidad: no enviar idCliente como prueba de propiedad. Los detalles ajenos no son accesibles.

## Solicitar nuevo servicio

Completar dirección, cantidad, tipo de baño solicitado, **Uso del servicio Obra/Evento** separado del tipo y fecha deseada. Se permite tipo Obra para uso Evento. Dirección principal del perfil no reemplaza la dirección obligatoria del pedido.

Tras Notificar/solicitar correctamente, se guarda Pendiente visible para administración; no crea servicios/entregas todavía. Administración asigna baños disponibles; Aprobada no significa entregado. Al completar todas las entregas pasa a Resuelta y se consultan los nuevos servicios y contador.

Otro baño, incluso en la misma dirección, se solicita por este mismo recorrido: servicios independientes, con sus propias frecuencias, tareas, traslados y bajas; sin adicionales vinculados.

## Acciones sobre servicio propio Entregado

- **Solicitar Limpieza Extra:** confirmación en el mismo detalle. Guarda Pendiente; administración aprueba/asigna y empleado cumple.
- **Realizar Reclamo:** descripción obligatoria no vacía, hasta 2000 caracteres; Notificar y Volver. Administración decide limpieza/mantenimiento y bloqueo, o responde/resuelve sin visita si no hay trabajos activos. Cliente no elige trabajo técnico.
- **Solicitar o informar cambio de dirección:** dirección actual, nueva dirección (hasta 300 caracteres, distinta de actual), modalidad. “Necesito que lo trasladen” requiere tarea física; dirección cambia al completarla. “Ya realicé el cambio” requiere fecha/hora efectiva Argentina no futura; cambia al validar el administrador, sin tarea ficticia. Historial anterior permanece.
- **Dar de Baja:** confirmar solicitud de finalización, no eliminación inmediata. Sigue vigente hasta retiro completado. Administración revisa trabajos pendientes; mantenimiento bloqueante puede conservar indisponibilidad posterior.

Una sola operación abierta traslado/baja por servicio, también si existe tarea activa administrativa. Si hay conflicto, se informa ID/estado; no volver a enviar como si no existiera.

## Reintentos, errores y sesión

Cada envío tiene ClaveEnvio: reintento conserva clave/contenido y recupera la misma solicitud; pedido nuevo usa otra. Clave ajena o reutilizada con datos distintos no permite acceder/duplicar. Durante envío se evita doble toque; éxito solo después de guardado. Ante fallo recuperable se conserva borrador en memoria; al cerrar sesión se limpia. Tras reinicio con resultado incierto, consultar Mis Solicitudes antes de crear otro pedido.

401 limpia sesión; 403 no es éxito ni lista vacía. Sin conexión se oculta identidad no verificada y se ofrece recuperación; no reutilizar datos del usuario anterior. Cerrar sesión elimina persistencia de token.

## Prueba manual por grupos

1. Cliente con servicios: contador, listado, detalle y volver. Otro sin servicios: vacío sin identidad/datos anteriores.
2. Nuevo pedido de dos baños: cliente Pendiente → administrador asigna → empleado primera entrega (Aprobada) y segunda (Resuelta) → cliente nuevos servicios/contador.
3. Limpieza y reclamo por separado: tarjetas propias → asignación/empleado → Resueltas. Otro reclamo sin trabajo: respuesta administrativa y fecha, sin tarea.
4. Traslado solicitado: comprobar origen/destino del empleado y revisión de otras tareas. Aviso ya realizado: fecha efectiva/validación sin tarea. Baja: retiro correcto, desaparece de servicios vigentes y actualiza contador.

Usar datos nuevos identificables y anotar IDs reales. Resultados ya confirmados, automatización y pendientes Android: [verificaciones](verificaciones.md).
