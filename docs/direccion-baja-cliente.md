# Cambio de dirección y baja del cliente

Rama `feature/direccion-baja-cliente`, solo API local y LocalDB. **Bloque cerrado y listo para commit:** verificaciones manuales Windows aprobadas según el cierre final del 08/11/2026. Las adaptaciones de fecha/hora efectivas y revisión administrativa fueron autorizadas por el estudiante; actualizará Figma. No se modifican los originales.

## Reglas confirmadas

Cliente activo, servicio propio **Entregado** y ubicación actual válida. Dirección nueva obligatoria, hasta 300 caracteres, espacios exteriores recortados y distinta de la actual. Dos modalidades separadas:

- **Necesito que lo trasladen (Solicitado):** enviar deja el pedido Pendiente; administración aprueba y asigna empleado/fecha, preparando destino sin activarlo. El empleado registra y finaliza el traslado. Solo al finalizar se cierra la ubicación anterior, se activa el destino y se resuelve la solicitud por la lógica de trabajos existente.
- **Ya realicé el cambio de dirección (Informado):** requiere fecha y hora efectivas no futuras, ingresadas como hora Argentina UTC-3. El envío no actualiza la ubicación registrada. Administración usa **Validar aviso**, con explicación **opcional**, para actualizar el historial con la fecha efectiva informada y resolverlo, sin tarea física. La fecha de registro/validación sigue siendo la real del servidor. No se admiten fechas anteriores al inicio de la ubicación actual, porque superpondrían intervalos anteriores; no se reparan historiales silenciosamente.

Antes de hacer efectivo el cambio, administración decide explícitamente para cada otra tarea **Pendiente y sin trabajo registrado** si conserva su referencia o pasa a la nueva ubicación. No hay opción predeterminada. Realizadas/Canceladas permanecen intactas. Las En curso o con trabajo registrado conservan su referencia e **impiden continuar** hasta resolverse; se muestran ID, tipo, estado y ubicación de cada una. No se cancelan automáticamente.

En Informado, la revisión y la validación se guardan juntas. En Solicitado, la revisión se guarda después de asignar el traslado; no cambia direcciones hasta su finalización. El cierre comprueba que tareas/ubicaciones/estados no hayan cambiado; una tarea nueva o iniciada obliga a revisar otra vez. El traslado que se está completando se excluye de sus propios bloqueantes. Si no existen otras tareas pendientes, no hay decisiones de ubicación que exigir. Las tareas históricas siguen apuntando a su ubicación original aunque el aviso sea retroactivo.

**Baja:** Dar de Baja abre una confirmación en el detalle. Guarda Baja/Pendiente, sin borrar el servicio ni liberar el baño. Administración programa retiro, reutilizando la solicitud. El módulo existente de Baños y Servicios mantiene revisión explícita de pendientes: no cancela En curso, trabajos registrados ni mantenimiento bloqueante. El empleado registra/finaliza retiro; entonces servicio Finalizado, solicitud resuelta cuando corresponde y disponibilidad según condición/mantenimiento. Un mantenimiento bloqueante Pendiente se conserva y sigue bloqueando después del retiro.

Se admite **una sola operación abierta de cambio de dirección o baja por servicio**: solicitudes Pendientes/Aprobadas y tareas activas Traslado/Retiro, incluso administrativas sin solicitud. El rechazo identifica qué solicitud/tarea y estado impiden continuar. Se protege también la asignación administrativa y la programación directa de retiro. Para solicitudes de baja múltiples heredadas, se conserva la elección explícita del retiro existente: no se crean bajas nuevas, no se cancelan otras silenciosamente.

## Persistencia y reintentos

Cliente, servicio, modalidad, nueva dirección, fecha y ubicación de origen forman parte del envío; se usa **ClaveEnvio** y el bloqueo SQL común de cumplimiento/asignaciones para ordenar operaciones simultáneas. Misma clave y datos recupera el pedido original, incluso después de resolver; otra clave es un pedido nuevo y vuelve a validar conflictos/estado. Clave ajena o con datos distintos devuelve 409 sin revelar la solicitud ajena. No se confía en un idCliente enviado por la app. Listado/detalle propios y permisos administrativos siguen protegidos por cuenta/rol actuales.

La app conserva texto/selección/fecha/clave en memoria de sesión por usuario/servicio ante errores. Durante resultado incierto bloquea modificar contenido y ofrece reintentar. Un 400 permite corregir. Ante conflicto explícito, puede descartarse el borrador y consultar el servicio otra vez, sin cancelar ninguna solicitud guardada; antes se debe revisar Mis Solicitudes. Tras éxito, otro pedido intencional genera nueva clave. Cerrar sesión limpia borradores; después de reiniciar con resultado incierto, consultar el listado antes de crear otro.

### Migración local

`20261107211946_RevisionUbicacionSolicitud`: agrega **solo** `SOLICITUDES_CLIENTE.RevisionUbicacionJson nvarchar(max) NULL`, sin otra tabla. Explicada antes de modificar el modelo/aplicarla. Conserva las **12 tablas de negocio**; `__EFMigrationsHistory` sigue siendo la tabla técnica de EF.

La columna guarda una instantánea técnica: origen/destino, clave y fecha de revisión, tareas/ubicaciones revisadas y elección de mover/conservar. Evita guardar decisiones futuras en Observaciones o cambiar una tarea de lugar antes del traslado. Se valida contra los datos actuales al completar. No se usa como un estado de negocio ni se devuelve al cliente. Las solicitudes anteriores mantienen NULL; no se reescribe su historial. La explicación administrativa opcional se conserva sin borrar una respuesta anterior por omitirla.

Aplicada únicamente a `(localdb)\MSSQLLocalDB/BanosQuimicosDb`. Antes/después de migrar: 6 usuarios, 8 solicitudes, 6 servicios, 6 ubicaciones y 14 tareas; las ocho solicitudes anteriores siguen sin revisión. **Pendiente de hosting:** esta migración, `ClaveEnvioSolicitud` y otras previas que falten se revisarán/aplicarán antes de la publicación final, después de Android integral. No se contactó ni modificó MonsterASP.

## Datos locales para Windows

Se ejecutó `scripts/preparar-direccion-baja-local.sql`, con guardas de nombre de base/LocalDB. Reutiliza Cliente Prueba y PRUEBA CUENTAS Windows activos, sin cambiar sus credenciales o roles. Prepara solo registros nuevos con marca `PRUEBA DIRECCION BAJA CLIENTE - SOLO LOCAL`:

- **PRUEBA-DIR-SOL / servicio 7:** origen propio, tareas Pendientes **15 y 16**, limpiezas semanales, para elegir una a mover y otra a conservar.
- **PRUEBA-DIR-INF / servicio 8:** origen propio, tareas Pendientes **17 y 18**, para la misma revisión al validar el aviso.
- **PRUEBA-BAJA-CLI / servicio 9:** limpieza Pendiente **19** y mantenimiento bloqueante Pendiente **20**; permite cancelar explícitamente la limpieza y conservar el bloqueo después del retiro.

No crea las solicitudes ni los traslados/retiros: quedan para la app. Después: mismos 6 usuarios/8 solicitudes, 9 servicios/9 ubicaciones/20 tareas. Los seis servicios y catorce trabajos anteriores permanecen. Repetir el script insertó cero registros persistentes y no restablece ubicaciones/estados ni trabajos realizados.

## Guía manual Windows

1. Abrir solución en Visual Studio 2026. Detener la API anterior si bloquea los archivos de compilación; iniciar la versión actual con perfil HTTPS local. App Windows Debug con `UsarHosting = false` (`https://localhost:7292/`). Usar las credenciales locales conocidas de Cliente Prueba, administrador y PRUEBA CUENTAS Windows. No cambiar contraseñas ni roles.
2. **Solicitado:** como cliente → Mis Servicios → PRUEBA-DIR-SOL → Solicitar o informar cambio → dirección `PRUEBA LOCAL - Destino solicitado 456` → Necesito que lo trasladen → Notificar. Mis Solicitudes debe indicar Pendiente y esa modalidad; el detalle sigue mostrando origen. Anotar el número real de solicitud.
3. Como administrador → Solicitudes y Reclamos → ese ID → aprobar/asignar traslado a PRUEBA CUENTAS Windows y fecha elegida. En Revisión de ubicación elegir tarea **15 pasar a nueva** y **16 conservar**; confirmar revisión. El servicio aún mantiene origen. Como empleado, abrir la tarea Traslado recién generada y verificar **ID de tarea, servicio, solicitud, Dirección de origen y Dirección de destino antes de Registrar Trabajo**. Registrar y finalizar solo ese traslado. Volver a abrirlo: ambas direcciones históricas deben conservarse. Como cliente: solicitud Resuelta y nueva dirección. La tarea 15 pasa al destino; 16 y el traslado conservan sus referencias de origen. No se completan automáticamente las limpiezas.
4. **Informado:** como cliente → PRUEBA-DIR-INF → cambio de dirección → `PRUEBA LOCAL - Destino informado 789` → Ya realicé el cambio. Elegir fecha/hora reales de prueba anteriores al momento actual y posteriores al inicio del origen mostrado (preparado dos días antes). No puede ser futura. Notificar: Pendiente, sin cambiar la dirección registrada todavía.
5. Como administrador → ese aviso → Revisión: tarea **17 pasar a nueva**, **18 conservar**. Dejar explicación vacía para verificar que es opcional o escribir una propia. Confirmar Validar aviso. Debe quedar Resuelta sin tarea Traslado; cliente ve nueva dirección, fecha efectiva informada e historial conservado. No confundir esa fecha con la validación actual.
6. **Bloqueantes opcionales para comprobar:** antes de revisar una dirección, registrar como empleado una de sus otras limpiezas, sin finalizarla. Administración debe mostrar exactamente esa tarea como impedimento; el cambio no se confirma. Finalizar dicha limpieza, consultar otra vez y decidir sobre las demás. Si después de una revisión se crea otra tarea, el cierre del traslado debe pedir revisión nueva. No modificar tareas anteriores a estos ejemplos.
7. **Baja del escenario 9:** como Cliente Prueba → PRUEBA-BAJA-CLI → Dar de Baja → confirmar y anotar el ID real de solicitud. Debe aparecer Pendiente y el servicio seguir vigente. Como administrador → Baños y Servicios → servicio 9 → Baja: elegir esa solicitud, empleado y fecha; confirmar y anotar el nuevo ID de tarea **Retiro**. La limpieza 19 ya está Realizada según el diagnóstico del 08/11: **no cancelarla ni reiniciarla**. Conservar mantenimiento bloqueante 20; revisar solo otros trabajos que aún estén pendientes. Como empleado, comprobar que el retiro muestra servicio 9 y la solicitud anotada, registrar/finalizar **ese retiro**, no la limpieza 19. Cliente Prueba: Baja Resuelta y servicio fuera de Mis Servicios/contador. Administración: servicio Finalizado; baño sigue bloqueado por mantenimiento 20. No marcarlo Apto ni completar/cancelar mantenimiento por omisión.
8. Antes de resolver un traslado o baja, intentar otro pedido incompatible sobre el mismo servicio: debe informar la solicitud/tarea y estado que lo impide, sin duplicar. Actualizar Mis Solicitudes. Reintentar el mismo envío no debe crear tarjeta nueva. Una vez terminado, otro pedido intencional necesita nueva clave y volver a validar el estado.

**Traslado informado aprobado manualmente por el estudiante.** No se extiende esa aprobación al traslado solicitado ni a la baja: se mantienen pendientes de repetir. Android integral local al finalizar bloques, usando la configuración USB existente (`adb reverse tcp:5148 tcp:5148`); luego migraciones y republicación. Continúan pendientes vencimiento real del token y alternancia de servidores. Las comprobaciones automatizadas no equivalen a ejecutar la app nativa.

## Verificaciones y cierre

- **11 pruebas nuevas** en DireccionBajaClientePruebas para aviso sin tarea/historial/fecha, traslado con decisiones/revisión desactualizada/bloqueantes, baja conectada al retiro, concurrencia y conflictos entre accesos, propiedad/validaciones/reintentos y seis casos de permisos.
- **17 casos afectados correctos:** 11 nuevos + 6 existentes de asignación/traslado. La prueba existente de traslado ahora hace la revisión explícita de su tarea auxiliar; conserva sus aserciones de resolución/historial, sin eliminar cobertura.
- **Una única suite completa: 188 correctos, 0 fallidos, 0 omitidos**, incluidos los 177 anteriores. Total **205 ejecuciones**, 188 casos distintos. Sin pruebas visuales simples nuevas ni repetición por documentación.
- API y componentes Razor compilaron con pruebas en bases LocalDB aisladas nuevas, conservadas. Windows y Android compilaron con **0 errores/advertencias**. Hubo comprobación incremental final tras completar conservación de respuesta opcional y manejo explícito de borradores en conflicto. Sin instalaciones/actualizaciones de herramientas.

El cierre inicial fue reabierto por la revisión del 08/11/2026 descrita a continuación y cerrado nuevamente tras la confirmación manual final. Sin commit, push, merge ni publicación. Mensaje sugerido: `feat: solicitar cambios de dirección y baja con revisión de tareas`.

Revisión final: diff sin errores de espacios, índice vacío y 194 candidatos no ignorados sin generados, perfiles privados ni indicadores de secretos en las rutas/textos examinados. PDF y dos PNG originales conservan sus hashes. No se repiten pruebas o compilaciones por este cierre documental.

## Correcciones y diagnóstico del 08/11/2026 — antecedentes de la repetición

**Aprobación manual:** únicamente el traslado informado como ya realizado, según confirmación del estudiante. LocalDB registra solicitud **10**, servicio **6**, Resuelta y validada. No se atribuyen otras aprobaciones a este mensaje.

**Destino omitido:** el DTO y el detalle del empleado solo mostraban Direccion (origen), aunque el modelo ya conservaba IdUbicacionDestino. Se amplió el contrato/API y su deserialización en la app con origen/destino e IDs de ubicación, servicio y solicitud. El detalle de Traslado muestra ambas direcciones antes de registrar y después de finalizar. La fuente son las relaciones de la tarea, nunca la dirección actual del servicio. Sin migración nueva.

Ejemplo real para repetir sin reiniciar: **tarea 21 / Traslado Realizada / solicitud 9 / servicio 6 PRUEBA-RETIRO-01**. Origen ubicación **6 (Pampa 315)** y destino **10 (Pampa 360)**. El servicio pasó posteriormente a ubicación **11 (Pampa 880)** por el aviso informado: esa dirección posterior no debe sustituir las dos referencias de la tarea 21. Para probar antes de realizar, usar el escenario 7 de los pasos 2-3, aún sin traslado creado en el diagnóstico; anotar sus nuevos IDs.

**Baja: datos comprobados, sin cambiar estados SQL.**

- **PRUEBA-BAJA-CLI / servicio 9 / Cliente Prueba (usuario 2, perfil 1):** Entregado, sin fechas de retiro/cierre, sin solicitud de baja y sin tarea Retiro. La tarea **19 es Limpieza semanal**, Realizada, con trabajo/finalización registrados el 08/11; no da de baja el servicio. La **20 es Mantenimiento bloqueante Pendiente**, sin trabajo/finalización. Un mantenimiento bloqueante Pendiente puede conservarse después del retiro; no exige cancelar esa reparación para cerrar.
- **Solicitud 11 / Baja Aprobada / servicio 6 PRUEBA-RETIRO-01 / Agus Tin (usuario 5, perfil 4):** su retiro correcto es la **tarea 22**, Pendiente y vinculada a solicitud 11/servicio 6, sin FechaTrabajoRegistrado, autor de registro ni FechaRealizada. Tampoco hay FechaRetiroReal/FechaFinalizacion del servicio. La consulta de trabajos que bloquean ese retiro devuelve **cero**. Se debe registrar y finalizar la tarea 22, y consultar después como **Agus Tin**, no como Cliente Prueba.
- **Retiro anterior 11 / PRUEBA-RETIRO-02 / servicio 5:** Realizada y sin solicitud; es otro trabajo. No se reabre ni se reutiliza para cerrar los servicios 6 o 9.

No se encontró en estos registros un cierre de retiro persistido que dejara sin actualizar la baja: falta completar el retiro correcto, y en el servicio 9 falta aún crear y programar la baja. Se reproduce en una base de prueba aislada que completar una limpieza no cierra un servicio y que registrar/finalizar el retiro vinculado sí actualiza solicitud, listado y contador. No se cambian reglas para aparentar la baja.

**Corrección de identificación y bloqueantes:** listado/detalle del empleado ahora muestran ID de tarea, servicio, solicitud, tipo y estado. Las limpiezas advierten que no completan un retiro. Para Retiro se consultan y muestran los ID/tipo/estado de los trabajos que impiden finalizar; la respuesta 409 identifica esos mismos trabajos y explica su revisión administrativa, sin cancelación automática. Mantenimiento bloqueante Pendiente permanece excluido de esos impedimentos según la regla existente. Si el trabajo es En curso o ya registrado, se debe completar; solo pendientes sin registro que no correspondan pueden cancelarse explícitamente desde Baños y Servicios.

**Pruebas focalizadas de esta corrección:** dos regresiones nuevas (contrato/API/app/detalle de traslado antes/después; limpieza frente a retiro correcto, bloqueo concreto y actualización de Mis Solicitudes/Mis Servicios/contador). Se ejecutaron **28 casos afectados, todos correctos**, reutilizando los de retiros/dirección y consulta de empleado. Ahora hay **190 casos definidos**; las 188 pruebas de la suite completa anterior son evidencia previa, no una nueva ejecución completa de 190. No se repite la suite completa por esta revisión focalizada.

LocalDB volvió a presentar el estado Stopped mientras un motor mantenía abiertos sus archivos. Se diagnosticó por lectura, se comprobó que no había otras sesiones/transacciones de usuario y se hizo apagado normal y arranque por nombre, siguiendo `docs/localdb.md`. Se volvió a consultar por `(localdb)\MSSQLLocalDB`; no quedó un canal temporal en ninguna configuración, ni se recreó instancia/base. En BanosQuimicosDb solo se leyeron datos de negocio: no se ejecutó preparación ni se modificaron estados para pasar la prueba.

En ese momento quedaban pendientes repetir el detalle del traslado y finalizar el retiro correcto comprobando IDs y cliente titular. Esas verificaciones fueron aprobadas en el cierre final siguiente. Android integral y publicación/migraciones del hosting siguen para el final. Sin commit, push, merge ni cambios en hosting.

La app Debug Windows y Android de esta corrección compiló con **0 errores y 0 advertencias**; API y Razor de pruebas también compilaron. No se repiten pruebas ni compilaciones por esta actualización documental. Para probar, reiniciar la API local con la versión corregida y abrir la app Windows actualizada; no usar un proceso anterior.

## Cierre final: pruebas manuales Windows aprobadas

El estudiante confirmó el 08/11/2026:

- En **Traslado 21**, el empleado ve las direcciones de origen y destino.
- En **Retiro 22**, registró el trabajo y finalizó la tarea correcta.
- Como **Agus Tin**, verificó la baja: el servicio dejó de aparecer en Mis Servicios y no quedan servicios vigentes.
- El **traslado Informado** conserva la aprobación manual ya confirmada.

Evidencia **manual**, separada de las pruebas automatizadas: suite completa anterior de 188 casos y revisión posterior de 28 casos afectados con 2 regresiones nuevas. No se afirma haber ejecutado una suite completa de 190, ni se atribuye a esta confirmación una baja del escenario 9 u otras pruebas no informadas. Los datos del diagnóstico anterior son antecedentes, no estados actuales a restablecer.

**Bloque cerrado y listo para commit.** Se actualizaron únicamente plan y guía, sin repetir pruebas ni compilaciones. `20261107211946_RevisionUbicacionSolicitud` continúa **pendiente de aplicación al hosting**. Android integral local y posterior republicación/migraciones siguen para el final; vencimiento real del token y alternancia de servidores continúan pendientes. Sin commit, push, merge, cambios en hosting ni inicio de otro bloque. Mensaje sugerido: `feat: solicitar cambios de dirección y baja con revisión de tareas`.
