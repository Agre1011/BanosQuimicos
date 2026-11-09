> Documento histórico archivado el 08/11/2026. Conserva decisiones, resultados y pendientes de su etapa; no es una guía vigente. Las actualizaciones posteriores pueden sustituir sus reglas. Consultar el [plan actual](../plan-trabajo.md) y el [índice histórico](indice.md).

# Reclamos: respuesta administrativa sin trabajo físico

Rama `feature/respuesta-reclamos`, desde master limpio `cb4b0a8`. API local/LocalDB; sin cambios en hosting. Se adapta el detalle existente de Solicitudes y Reclamos con campo y confirmación autorizados, sin pantalla nueva ni cambios en originales.

## Recorrido y reglas

Para un Reclamo Pendiente o Aprobado, el administrador activo puede **Responder y resolver**, escribir una respuesta obligatoria (1 a 2000 caracteres, sin aceptar espacios solos) y confirmar. La API guarda texto y fecha de resolución y cambia a **Resuelta**; no crea tareas ni altera servicios, ubicaciones, condición, responsables habituales o trabajos anteriores. Si necesita trabajo físico, sigue disponible **Asignar ▾ → Limpieza extra / Mantenimiento**, con las reglas y resolución por cumplimiento existentes.

Se reutilizan `SOLICITUDES_CLIENTE.ObservacionesAdministrador` y `FechaResolucion`, y el catálogo Solicitud/Resuelta (ID 4). **Sin migración ni tablas nuevas**: permanecen las 12 del negocio. No se reemplaza la descripción original del cliente ni una explicación administrativa previa distinta. No se implementa edición de respuestas, rechazo o cancelación, ni cierre de otros tipos.

No se permite responder y resolver si tiene tareas vinculadas Pendientes/En curso o trabajo registrado sin finalizar, aun en un registro inconsistente de tarea cancelada. El error identifica ID, tipo y estado; la acción no cancela ni omite el trabajo. Las tareas terminales anteriores sin cumplimiento inconcluso permanecen intactas. Un reclamo ya cerrado no admite una respuesta distinta ni se reabre desde esta acción.

Se usa el mismo bloqueo transaccional Serializable de asignación/cumplimiento para ordenar respuesta frente a creación o finalización de tareas y revalidar al administrador. Un reintento con el mismo ID y texto normalizado recupera el cierre existente sin reemplazar texto/fecha; una respuesta distinta posterior devuelve conflicto. No requiere otra ClaveEnvio: cada reclamo tiene un único cierre y la respuesta guardada es inmutable en esta acción. El estado anterior esperado impide cerrar silenciosamente un contexto que cambió.

La app bloquea doble toque y conserva texto/envío incierto en memoria por administrador/reclamo durante la sesión; ante error de conexión reintenta el mismo contenido. Un rechazo explícito permite corregir, manteniendo el texto. Cerrar sesión limpia borradores; no se guardan en disco. Tras reiniciar con resultado incierto, consultar el detalle antes de intentar otra respuesta.

El cliente ve **Respuesta administrativa**, fecha y Resuelta en su tarjeta de Mis Solicitudes al actualizar. Solo se incorpora la respuesta de Reclamo resuelto, no notas administrativas de otras operaciones. Lista y detalle mantienen el filtro de propietario desde la sesión, sin confiar en un idCliente enviado. El detalle ajeno sigue dando 404 y no se exponen contraseñas, hashes ni tokens.

## API

`POST /api/solicitudes/{id}/responder-resolver`, Bearer de administrador activo:

```json
{
  "respuesta": "PRUEBA RESPUESTA LOCAL: se aclara lo consultado; no requiere trabajo físico.",
  "idEstadoAnterior": 1
}
```

`idEstadoAnterior`: 1 Pendiente o 2 Aprobada, según el detalle consultado. 200: detalle real guardado (también en reintento idéntico); 400: datos inválidos/extra; 401: sin sesión válida; 403: sin administrador activo; 404: inexistente; 409: estado/tipo incompatible, explicación previa distinta o trabajos que impiden el cierre. La API comprueba permisos y trabajos aunque se invoque directamente, sin depender de ocultar botones.

GET `/api/solicitudes-cliente/mias` y `/api/solicitudes-cliente/{id}` agregan `respuestaAdministrativa` y `fechaResolucion` a sus contratos propios. No hay ruta para que el cliente escriba la respuesta. El DTO administrativo ya contenía los campos usados.

## Guía breve Windows — tres recorridos aprobados según el cierre

1. Ejecutar la API local con perfil https y BanosQuimicos.App/Windows desde Visual Studio 2026. `ConexionApi` conserva `UsarHosting=false` y https://localhost:7292. Usar cuentas locales conocidas, sin modificar sus contraseñas ni roles ni tocar MonsterASP. El registro nuevo es Cliente/Activa por el bloque previo, no necesita aprobación.
2. Como cliente con un servicio propio **Entregado**: Mis Servicios → detalle → Realizar Reclamo. Escribir **PRUEBA RESPUESTA LOCAL: necesito una aclaración del servicio** y Notificar. Anotar su número real en Mis Solicitudes; debe estar Pendiente. Si ese cliente no tiene un servicio entregado, primero usar el recorrido normal de pedido/asignación/entrega; no restablecer servicios anteriores por SQL.
3. Como administrador: Solicitudes y Reclamos → detalle del número anotado → Responder y resolver. Respuesta vacía no permite confirmar. Escribir **PRUEBA RESPUESTA LOCAL: aclaración recibida, no requiere visita**, cancelar una vez (sin guardado) y luego confirmar. Debe mostrar respuesta, fecha y Resuelta, sin crear tareas. Actualizar/reabrir para comprobar persistencia; el recorrido de asignación no debe ofrecer crear trabajo nuevo para ese reclamo cerrado.
4. Volver como el cliente: Mis Solicitudes → Actualizar consulta. Debe mostrar ese reclamo Resuelto con el texto exacto de la respuesta. Descripción original, servicio y baño permanecen sin cambios. Otro cliente no debe recibir ese reclamo ni su respuesta (aislamiento comprobado automáticamente; la comprobación visual adicional es opcional y no se declara realizada).
5. **Bloqueo:** crear un segundo reclamo nuevo, identificado **PRUEBA RESPUESTA BLOQUEADA LOCAL**. Como administrador, aprobar y asignar limpieza o mantenimiento a un empleado activo por el recorrido existente y anotar IdTarea. Después intentar Responder y resolver: debe mostrar el trabajo bloqueante, sin resolver, responder ni cancelar. Como empleado, puede Registrar Trabajo y volver a comprobar el bloqueo antes de Actualizar Estado. Finalizar por el recorrido normal debe resolver la solicitud por cumplimiento; no añadir otra respuesta para simular un cierre sin trabajo. No reiniciar ni cancelar trabajos anteriores para preparar este escenario.

Estos pasos generan reclamos nuevos identificables mediante texto/ID. No se insertaron datos operativos en la base del estudiante: las pruebas automatizadas usan bases LocalDB aisladas BanosQuimicosAuthPruebas_GUID y no borran bases previas.

## Evidencias y pendientes

Ver sección 29 del plan para los resultados reales de pruebas afectadas y compilaciones. No se ejecuta la suite completa por este cambio; se conservan las pruebas existentes. Los tres recorridos manuales Windows están aprobados según el cierre. Android integral contra API local y posterior correcciones/migraciones/republicación quedan para el final. Vencimiento real del token y alternancia de servidores siguen pendientes generales. Este bloque no agrega migraciones, y las de bloques previos pendientes para hosting mantienen ese estado. Sin commit, push, merge ni cambios en hosting.


Verificaciones ejecutadas: **13 casos nuevos; 60/60 afectados correctos** (47 existentes conservados). API/Razor y app Debug Windows/Android compilaron correctamente, **0 errores y 0 advertencias**. En esa ejecución no se ejecutó la suite completa; la confirmación manual posterior está registrada por separado en el cierre. 230 casos definidos en total; la evidencia completa anterior sigue siendo 217/217. Los tres recorridos manuales Windows fueron aprobados posteriormente, según el cierre.


## Cierre final: pruebas manuales Windows aprobadas — 08/11/2026

El estudiante confirmó estos **tres recorridos manuales Windows**:

1. Responder y resolver un reclamo, con validación de respuesta obligatoria.
2. Consultar como cliente el estado Resuelto, la respuesta y su fecha, sin generar una tarea.
3. Impedir el cierre administrativo cuando hay una tarea activa, mostrando el bloqueo y conservando los registros.

Esta evidencia manual se registra por separado de las **60/60 pruebas automatizadas afectadas aprobadas** (13 nuevas y 47 existentes). **No se ejecutó la suite completa en este bloque**; 230 es el número de casos definidos, no una suite completa ejecutada. Las compilaciones previas Windows/Android correctas (0 errores/advertencias) conservan su evidencia; no se repiten pruebas ni compilaciones por este cierre exclusivamente documental. No se atribuyen otras verificaciones manuales no informadas.

**Bloque feature/respuesta-reclamos cerrado y listo para commit.** No agrega migración ni cambia las 12 tablas. Se mantienen pendientes Android integral contra API local, correcciones y posterior migraciones/republicación/comprobación de hosting; vencimiento real del token y alternancia de servidores continúan pendientes generales. Las migraciones anteriores pendientes del hosting conservan ese estado. Sin commit, push, merge, cambios en hosting ni inicio de otro bloque.

Mensaje de commit sugerido: **feat: resolver reclamos con respuesta administrativa sin tareas**.

Descripción breve sugerida del PR: «Agrega respuesta obligatoria y confirmación para resolver reclamos sin crear tareas, y muestra al cliente la respuesta, fecha y estado. Bloquea el cierre cuando hay trabajos activos y protege permisos, historial y reintentos, conservando la asignación de limpieza/mantenimiento. Verificado con 60 pruebas automatizadas afectadas y tres recorridos manuales aprobados en Windows; sin migración nueva. Android integral y republicación quedan pendientes.»
