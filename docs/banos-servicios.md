# Baños y Servicios: baja y retiro

## Recorrido y reglas confirmadas

Inicio del administrador → Baños y Servicios muestra tarjetas basadas en Figma, con código del baño, tipo físico/uso, cliente, dirección, frecuencia referencial, estado y responsable habitual de limpieza. Este último pertenece al baño y no se confunde con el empleado de cada entrega, retiro u otro trabajo. Se mantienen servicios finalizados/cancelados y sus tareas como historial. Incluye carga, vacío, error, consulta y Volver.

**Baja programa un retiro; no borra ni libera el baño.** Solo aplica a servicios Entregados. Una cancelación antes de entregar es otra operación: no se registra un retiro ficticio ni se amplía este bloque para implementarla.

El administrador elige empleado activo y fecha y confirma la programación. Si hay una baja Pendiente/Aprobada del servicio se muestra y vincula; con varias, se elige explícitamente. Solo la elegida se aprueba. Si no hay ninguna, se crea el retiro administrativo sin inventar una solicitud del cliente. Las demás solicitudes conservan sus estados.

Se confirmaron estas reglas para los otros trabajos:

- El retiro puede programarse aunque existan otras tareas pendientes.
- Antes del cierre, **Revisar otras tareas** permite marcar las pendientes que ya no corresponden y confirmar qué se cancela y qué se conserva para completar. No hay cancelación automática ni obligación de realizar una limpieza futura que ya no corresponde si se cancela explícitamente.
- Las tareas En curso deben completarse antes del cierre. Tampoco se cancela un trabajo con cumplimiento ya registrado: se conserva su registro y debe finalizar su estado.
- Mantenimientos bloqueantes Pendientes se conservan, incluso si están vinculados al servicio, y pueden continuar después del retiro. Siguen impidiendo disponibilidad. Si están En curso, deben completarse antes del cierre como los demás trabajos En curso.
- Cancelar guarda estado Cancelada y FechaCancelacion, conservando empleado, contexto, observaciones e historial. No cancela ni resuelve solicitudes vinculadas. Lo conservado mantiene su estado; no se agrega un estado ficticio ni se requiere una tabla de decisiones.

La API vuelve a comprobar esas condiciones al finalizar el retiro. Mientras queden otros trabajos Pendientes/En curso que deben resolverse, devuelve 409 y conserva servicio/asignación. El mantenimiento bloqueante Pendiente es la excepción que se preserva para después. Al cerrar se guardan FechaRetiroReal/FechaFinalizacion y servicio Finalizado, liberando su asignación vigente. La disponibilidad actual sigue dependiendo de Apto, ausencia de otra asignación vigente y ausencia de mantenimiento bloqueante activo. No se cambia automáticamente la condición física.

La solicitud de baja vinculada se resuelve con las reglas existentes de trabajos requeridos. Si tiene otros trabajos cancelados o sin completar, no se omiten para aparentar resolución. Las solicitudes de otros trabajos cancelados quedan para revisión administrativa. Para mantener el historial y permitir reparar el baño retirado, una tarea de mantenimiento vinculada a un servicio Finalizado puede registrarse/finalizarse; no se desvincula del servicio ni se crea otro ficticio.

## API y persistencia

Sin migración nueva ni cambios de entidades: se reutilizan las **12 tablas**, ClaveProgramacion, estados, FechaCancelacion y relaciones existentes.

- GET /api/servicios y /api/servicios/{id}: datos administrativos seguros, tareas y solicitudes de baja, con orden estable por código/servicio. La dirección es la vigente del servicio o la última referencia disponible; no se redirigen tareas históricas. La disponibilidad se calcula para el baño considerando todas sus asignaciones, no solo la tarjeta que se consulta.
- POST /api/servicios/{id}/retiro: claveProgramacion, idEmpleado, idUbicacionEsperada, idSolicitudBaja/idEstadoSolicitudAnterior opcionales y fecha. Devuelve la tarjeta actual. Misma clave/datos recuperan el retiro; otra clave no crea un segundo retiro del servicio. También se protege la ruta existente de asignación de solicitudes frente a un retiro ya programado desde esta vista.
- POST /api/servicios/{id}/resolver-pendientes: idCancelar e idConservar, con IDs distintos y todas las tareas pendientes del servicio, salvo el retiro. Se rechaza una selección desactualizada, IDs ajenos, cancelación de En curso/cumplimiento registrado/bloqueante o tareas cambiadas. Repetir una cancelación confirmada mientras continúa el retiro no cambia su fecha ni duplica efectos.
- Solo Administrador/Activa para consulta/programación/decisión. Se revalida dentro de transacciones Serializable con el mismo bloqueo SQL de cumplimiento/programación. Empleado de retiro debe seguir Empleado/Activa; al realizarlo se comprueba propiedad como en las tareas existentes. No hay DELETE, reasignación ni edición general añadidos.

## Datos locales nuevos

Se ejecutó scripts/preparar-banos-servicios-local.sql exclusivamente en LocalDB/BanosQuimicosDb, con comprobación de base/IsLocalDB y marcas reservadas. Reutiliza cliente y empleado de prueba existentes sin modificar sus cuentas/contraseñas. Preparó dos servicios nuevos, sin crear todavía sus retiros:

- **Servicio 4 · PRUEBA-RETIRO-01**: Entregado/Apto, dirección PRUEBA LOCAL - Retiro 1 123, solicitud de baja Pendiente y **tarea 7 Limpieza semanal** futura para revisión explícita.
- **Servicio 5 · PRUEBA-RETIRO-02**: Entregado/Apto, dirección PRUEBA LOCAL - Retiro 2 123, sin solicitud de baja y **tarea 8 Mantenimiento bloqueante** Pendiente, vinculada a este servicio para verificar que puede continuar después del retiro.

Las tareas nuevas están asignadas al empleado local PRUEBA CUENTAS Windows. No se copió ese empleado como responsable habitual. Se conservaron las 6 cuentas y los 3 servicios/6 trabajos/4 solicitudes anteriores. Después de la preparación hay 5 baños, 5 servicios, 8 tareas y 5 solicitudes. Repetir el script reutiliza sus registros sin reabrirlos ni restablecer estados. No usar los servicios anteriores para esta prueba de baja.

## Prueba breve Windows

1. Detener la API anterior, compilar y reiniciar desde Visual Studio con perfil https. Comprobar LocalDB/BanosQuimicosDb y UsarHosting=false. La app Windows usa https://localhost:7292/. No ejecutar contra hosting. No se necesita otra migración para este bloque.
2. Ingresar como administrador → Baños y Servicios → **PRUEBA-RETIRO-01**. Comprobar servicio Entregado y limpieza futura Pendiente. Pulsar Baja, verificar qué solicitud se atiende, elegir PRUEBA CUENTAS Windows y fecha, y confirmar. El servicio continúa Entregado y el baño no queda disponible todavía.
3. Abrir Revisar otras tareas. Marcar la limpieza 7 para cancelar si ya no corresponde y confirmar la decisión. Debe aparecer Cancelada en el historial, sin eliminar la tarea ni resolver solicitudes por ese solo hecho. Si se decide conservarla, tendrá que completarse antes del retiro; no se cancela por omisión.
4. Cerrar sesión e ingresar como el empleado. Abrir la tarea **Retiro de PRUEBA-RETIRO-01**, Registrar Trabajo y Actualizar Estado. Como administrador, consultar nuevamente: servicio Finalizado con fechas, baja Resuelta cuando no falten otros trabajos y baño Disponible si conserva Apto/sin bloqueos. El servicio y sus tareas siguen visibles.
5. Repetir programación en **PRUEBA-RETIRO-02**. Debe indicar retiro administrativo sin solicitud del cliente. En revisión, el mantenimiento 8 se conserva: no se ofrece cancelarlo. Completar el retiro como empleado. Resultado: servicio Finalizado y baño **No disponible — Mantenimiento bloqueante**, con tarea de mantenimiento todavía Pendiente.
6. Opcionalmente, completar como empleado ese mantenimiento después del retiro. Debe conservar vínculo/historial y levantar el bloqueo por su estado Realizada. La condición física no cambia; si fuera No apto seguiría sin disponibilidad. Conservar resultados como evidencia; no restablecer los ejemplos para repetir.

Se puede probar antes de la decisión administrativa el intento de cerrar el primer retiro: debe fallar mientras la limpieza siga pendiente, conservando el registro de trabajo ya guardado. Tras decidir cancelarla o completarla, el mismo retiro puede finalizarse sin otro registro ni duplicación. Validar visualmente ventanas reducidas, desplazamiento, confirmaciones y regreso en Windows.

## Verificaciones y pendientes

Se agregaron **13 casos automatizados** de reglas/riesgos, sin pruebas nuevas para cambios visuales simples. Se ejecutaron **20 casos afectados** durante el desarrollo (13 nuevos y 7 existentes) y una única suite completa de cierre de **147 casos**: todos correctos. Total: **167 ejecuciones de casos**. Las pruebas usan bases LocalDB aisladas, conservadas. No se repiten por cambios documentales.

Cubren cierre/resolución/fechas/historial, decisiones explícitas e idempotentes, trabajos En curso o con cumplimiento registrado, mantenimiento bloqueante después del retiro y reparación sin cambio físico automático, permisos, concurrencia, prevención de duplicados entre rutas, contexto inválido, múltiples bajas y selección desactualizada. Se ajustó solo la preparación auxiliar del caso existente de baja para no simular involuntariamente una limpieza sin atender; no se eliminaron pruebas ni se debilitaron sus verificaciones.

API/pruebas compilaron en tmp/pruebas-servicios. App Debug Windows y Android compilaron con **0 errores y 0 advertencias**, usando los parámetros temporales JavaMaximumHeapSize=512M y JavaOptions=-XX:+UseSerialGC ya documentados. No se cambiaron paquetes, herramientas ni configuración global. Estos resultados no se presentan como ejecución nativa de las pantallas.

Los dos recorridos manuales Windows fueron aprobados según el cierre siguiente. La integral Android será al terminar los bloques, contra API local; luego se planificarán migraciones/republicación del hosting. Compilar no equivale a prueba manual. Se conservan pendientes vencimiento real del token y alternancia entre servidores en el dispositivo. Sin cambios de hosting, commit, push o merge.

## Cierre manual confirmado

**Windows aprobado por el estudiante en ambos recorridos:**

- Primer retiro: Actualizar Estado bloqueó su finalización por una limpieza pendiente. El administrador canceló explícitamente esa limpieza y confirmó la revisión; el empleado pudo finalizar el retiro después de esa decisión. Se confirmó el resto del recorrido del caso.
- Mantenimiento bloqueante: el retiro se completó y el baño conservó el bloqueo de disponibilidad después del cierre del servicio.

Esta confirmación manual se separa de las **147 pruebas automatizadas** y de las compilaciones ya registradas. La reparación posterior opcional no se marca como probada manualmente. No se repitieron pruebas ni compilaciones por esta actualización documental, ni se restablecieron los ejemplos: sus resultados quedan como historial.

**Bloque cerrado y listo para commit.** Integral Android contra API local y posterior republicación pendientes para el final. Sin migración nueva, cambios de hosting, commit, push ni merge.


## Actualización: paginación y contadores

La pantalla ahora consulta /api/servicios/pagina con páginas de diez, total y navegación. Las rutas de lista anteriores se conservan por compatibilidad; los filtros se aplican antes de paginar en la API y no a una página aislada. Ver [guía Windows actual por grupos](paginacion-contadores-logo.md). Las aprobaciones manuales anteriores no acreditan la paginación nueva.


Filtro paginado vigente: Todos / Baños de obra / Baños de evento, según tipo físico del baño relacionado con el servicio, independiente de su uso. Consulta /api/servicios/pagina con tipoBano=Todos/Obra/Evento. Cambiar filtro reinicia página1; actualizar/cerrar selección conserva filtro/página. Sin coincidencias muestra un aviso específico. Ver los dos ajustes pendientes de revisión manual en docs/paginacion-contadores-logo.md; no se declaran ya probados por las aprobaciones previas.
