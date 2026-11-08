# Limpieza extra y reclamos del cliente

Rama `feature/limpieza-reclamos-cliente`, API local y LocalDB. Mis Servicios → detalle de un servicio propio → Solicitar Limpieza Extra o Realizar Reclamo. Cambio de dirección y baja siguen deshabilitados.

## Comportamiento y reglas

Ambas acciones requieren **Cliente/Activa, propietario y servicio Entregado**, siguiendo la condición del recorrido administrativo existente. Pendiente de entrega, Finalizado y Cancelado no admiten pedidos nuevos de estos tipos. La condición física o un mantenimiento no borran ni ocultan una asignación; no se exige que el baño esté disponible para alquilarlo a otro cliente para poder reclamar o pedir limpieza sobre su servicio actual.

Limpieza extra abre una **confirmación dentro del detalle** con código/dirección y Confirmar/Cancelar. No solicita fecha, observaciones ni otros datos inventados: administración elige empleado y fecha al aprobar y asignar. Descripcion se guarda nula, sin simular observaciones.

Reclamo utiliza la referencia de Figma: descripción, **Notificar** y **Volver**. Texto obligatorio, hasta 2000 caracteres según el modelo, sin aceptar espacios en blanco; se recortan espacios exteriores. El cliente describe el problema; no elige tarea, empleado ni bloqueo. Administración decide Limpieza extra o Mantenimiento y, para mantenimiento, si bloquea asignaciones.

Ambas se guardan como **Pendiente**, vinculadas al cliente y servicio propios, con fecha del servidor. No crean trabajos ni cambian el responsable habitual. Mis Solicitudes y Solicitudes y Reclamos del administrador ya las consultan. Aprobar y asignar crea la tarea Pendiente y deja la solicitud Aprobada; se resuelve mediante los trabajos necesarios al finalizar, según la lógica existente. No se confunde enviar/aprobar con resolver.

## API, seguridad y reintentos

- `POST /api/solicitudes-cliente/servicios/{id}/limpieza-extra`: `claveEnvio`, sin descripción.
- `POST /api/solicitudes-cliente/servicios/{id}/reclamo`: `claveEnvio` y `descripcion`.

Primera creación: 201 con solicitud real. Reintento: 200 con la misma solicitud y estado actual. Se utiliza la columna/índice ClaveEnvio ya existente, transacción Serializable y el bloqueo de envíos anterior. Una clave solo corresponde a un cliente, servicio, tipo y descripción normalizada; reutilizarla con otros datos o desde otro cliente devuelve 409 sin revelar el pedido ajeno. Nueva clave significa otra solicitud intencional, incluso con contenido igual. Servicio ajeno/inexistente para un envío nuevo: 404. Cliente activo revalidado en la transacción; la API no toma un idCliente enviado por la app.

El reintento idéntico reconoce el guardado incluso si el servicio terminó después: no genera otro pedido ni reabre historial. Una clave nueva requiere Entregado en ese momento. La clave es compartida globalmente con nuevo servicio, sin permitir reutilizarla para otro tipo.

La app deshabilita envíos durante el procesamiento y no muestra éxito antes de confirmar guardado. El texto/clave se conservan en memoria de sesión por usuario/servicio/tipo, incluso al salir de la vista o al ocultarla por error de conexión. Si el resultado es incierto, bloquea cambiar el contenido y permite reenviar el mismo pedido. Un rechazo de validación permite corregir el texto. Tras éxito se limpia el borrador; abrir otra confirmación/formulario inicia otra solicitud con clave nueva. Cerrar sesión limpia borradores. No se guarda el texto en disco: tras reinicio con resultado incierto, revisar Mis Solicitudes antes de iniciar otro pedido.

**Sin cambios de modelo, tablas ni migración nueva.** Continúan las 12 tablas y el historial. ClaveEnvio usa la migración `20261107191916_ClaveEnvioSolicitud`, ya aplicada solo en LocalDB y todavía pendiente para el hosting junto con las demás actualizaciones finales. No se modificó MonsterASP ni la configuración local/HTTPS/USB.

## Guía manual Windows

1. Visual Studio 2026: iniciar API actualizada con perfil HTTPS local y app Windows Debug. `UsarHosting = false` se conserva: Windows `https://localhost:7292/`. Usar contraseñas conocidas, sin cambiar roles ni credenciales.
2. Como **Cliente Prueba**, abrir Mis Servicios → **PRUEBA-TAREAS-01 (servicio 1, Entregado)**. También sirven sus servicios 2/3 PRUEBA-NUEVO-01/02 si siguen Entregados. Otra cuenta disponible es **Agus Tin**, con PRUEBA-RETIRO-01/servicio 6 Entregado. Estas referencias provienen de una consulta local de solo lectura; no se prepararon ni resetearon trabajos.
3. Solicitar Limpieza Extra → Cancelar: no debe aparecer un nuevo pedido. Abrir de nuevo y Confirmar: debe mostrar ID real y Pendiente. Volver al inicio → Mis Solicitudes: tipo, baño, fecha y estado correctos. Anotar el ID sin asumir un número fijo.
4. Como administrador → Solicitudes y Reclamos → ID anotado → empleado activo **PRUEBA CUENTAS Windows**, fecha elegida → Aprobar y asignar. Como empleado, consultar la nueva tarea identificada por servicio/fecha → Registrar Trabajo → Actualizar Estado. Como cliente, actualizar Mis Solicitudes: **Resuelta**.
5. Volver al detalle como cliente → Realizar Reclamo. Probar texto vacío/espacios: impide notificar. Escribir `PRUEBA LOCAL CLIENTE - revisar puerta del baño`, Notificar y verificar Pendiente en Mis Solicitudes. Como administrador, abrirlo, elegir **Mantenimiento** e indicar bloqueo si corresponde, empleado y fecha → Aprobar y asignar. El habitual se conserva. Como empleado, consultar la descripción y completar ambos pasos. Como cliente, actualizar: **Resuelta**. El bloqueo de la tarea se levanta por finalización; no se cambia automáticamente la condición física.
6. Comprobar Volver del reclamo al detalle, cambio de dirección y baja deshabilitados. Para otro pedido intencional abrir nuevamente la acción después del éxito. Ante conexión incierta, conservar el envío y consultar Mis Solicitudes antes de crear otro. No cancelar o editar tareas anteriores para probar este bloque.

### Resultado manual Windows: ambos recorridos aprobados

El estudiante confirmó:

- El cliente creó una limpieza extra y un reclamo, cada uno con su propia tarjeta en Mis Solicitudes.
- Ambas solicitudes aparecieron para el administrador y permitieron asignar las tareas.
- El empleado recibió ambas tareas, registró el trabajo y las finalizó.
- Al volver al cliente, ambas solicitudes aparecieron **Resueltas**.

Esta evidencia es **manual**, separada de las **177 pruebas automatizadas anteriores**. No se atribuyen a esta confirmación validaciones de texto, cancelación de la confirmación, reintentos, aislamiento entre cuentas, pérdida/recuperación de conexión ni la elección específica de mantenimiento/bloqueo. Los pasos correspondientes de la guía conservan sus resultados esperados para comprobaciones adicionales.

Integral manual Android al finalizar los bloques contra API local; después migraciones/republicación. Mantener pendientes vencimiento real del token y alternancia de servidores en dispositivo. Sin commit, push, merge ni publicación.

## Verificaciones

Se agregaron **11 casos** para las nuevas entradas del cliente: dos conexiones completas al recorrido existente (limpieza extra/reclamo con mantenimiento), reintentos simultáneos y nueva solicitud intencional, propiedad/clave ajena/tipo alterado, descripción y estados del servicio, seis casos de permisos y conservación de borrador/clave. Se reutilizan las pruebas existentes de aprobación, selección de tarea del reclamo, bloqueo, empleado y resolución, sin reescribir esa lógica ni automatizar detalles visuales simples.

- Pasada focalizada: **11/11 correctos**.
- Una única suite completa de cierre: **177 correctos, 0 fallidos, 0 omitidos**, incluidos los 166 casos anteriores conservados.
- Total **188 ejecuciones de casos**, 177 casos distintos. No se repitieron por cambios documentales.
- API y componentes Razor enlazados compilaron con las pruebas, que usan bases LocalDB aisladas nuevas y conservadas. No se escribieron datos operativos en BanosQuimicosDb.
- App Debug Windows y Android: **0 errores y 0 advertencias**, con los parámetros temporales de Java ya documentados. Comprobación incremental final tras ajustar el texto de Volver para no sugerir cancelación de un envío incierto; sin nuevas pruebas visuales.

Diff y 182 candidatos no ignorados revisados: índice Git vacío, sin generados, perfiles privados ni indicadores de secretos en las rutas/textos examinados. Originales PDF/PNG con hashes conservados. Los recorridos manuales Windows aprobados se registran arriba; la integral Android sigue para el final. Este cierre solo modifica documentación, sin repetir pruebas ni compilaciones. **Bloque cerrado y listo para commit**, sin comenzar el siguiente, commit, push, merge ni cambios en hosting. Mensaje sugerido: `feat: solicitar limpieza extra y registrar reclamos del cliente`.

## Respuesta administrativa posterior

Además de generar trabajos, el administrador puede responder y resolver un reclamo que no los necesite. El cliente ve la respuesta y la fecha en Mis Solicitudes al actualizar; no se omiten tareas activas. Ver [respuesta de reclamos](respuesta-reclamos.md). La aprobación manual Windows previa de limpieza/mantenimiento no acredita esta función nueva.
