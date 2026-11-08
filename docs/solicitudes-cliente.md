# Solicitar nuevo servicio y Mis Solicitudes

**Alcance vigente (08/11/2026):** para necesitar otro baño se usa **Solicitar un nuevo servicio**, pudiendo repetir la dirección. No hay adicionales vinculados, agrupación de contrataciones ni herencia de datos. Los servicios resultantes son independientes, con sus propias frecuencias, tareas, traslados y bajas. Esta decisión reemplaza las propuestas previas de adicionales; no cambia el recorrido existente ni elimina tipos o registros históricos.


Rama `feature/solicitudes-cliente`. Solo API local y LocalDB. El cliente envía el pedido; administración selecciona baños y entregas en el recorrido existente. Enviar no crea servicios, ubicaciones ni tareas.

## Formulario y estados

Inicio → Solicitar un nuevo servicio: dirección obligatoria (hasta 300 caracteres, sin espacios exteriores), cantidad entera positiva (sin un tope arbitrario adicional al entero del modelo), tipo solicitado del catálogo, **uso Obra/Evento independiente del tipo**, fecha deseada obligatoria. El estudiante autorizó agregar Uso del servicio en la misma pantalla y actualizará Figma. Se conserva la posibilidad de tipo Obra para uso Evento. La frecuencia y empleado/fecha de entregas los elige administración, no este formulario.

La fecha deseada es referencial: no reserva unidades ni confirma entrega. Se valida que sea una fecha válida y no omitida; las reglas existentes no exigen que sea posterior a hoy y no se agregó esa prohibición. La API fija Cliente, Nuevo servicio, Pendiente y fecha de registro desde la sesión y el reloj del servidor. No acepta estados/roles elegidos por el cliente. La confirmación y número de solicitud aparecen solo después de recibir el guardado real.

Mis Solicitudes muestra todos los tipos propios, orden descendente por fecha e ID: tipo, número, fecha, descripción disponible, datos del pedido nuevo y códigos de baños realmente vinculados. Antes de asignar se indica «Sin baños asignados», sin inventar códigos. Usa los estados reales **Pendiente, Aprobada, Rechazada, Resuelta, Cancelada**. Aprobada no equivale a Resuelta; completar una de varias entregas no termina el pedido. Los indicadores muestran texto además de color. Incluye carga, vacío, error, actualizar y Volver; un error no se muestra como lista vacía. Sin edición, cancelación ni eliminación.

## Seguridad y reintentos

Rutas exclusivas Cliente/Activa, identidad obtenida del usuario autenticado:

- `GET /api/solicitudes-cliente/tipos-bano`: catálogo de tipos.
- `GET /api/solicitudes-cliente/mias`: solicitudes propias de todos los tipos.
- `GET /api/solicitudes-cliente/{id}`: propia o 404; no hay pantalla adicional de detalle en este bloque.
- `POST /api/solicitudes-cliente/nuevo-servicio`: claveEnvio, direccion, cantidadBanos, idTipoBano, tipoUso, fechaDeseada (`AAAA-MM-DD`). Creación 201; reintento idéntico 200 con la solicitud ya guardada y su estado actual.

Cliente/estado se vuelven a comprobar dentro de la transacción de escritura. El guardado serializa envíos concurrentes y el índice único refuerza la clave. Reutilizar una clave con otro cliente o datos distintos devuelve 409 sin mostrar el pedido ajeno. Una nueva clave permite un pedido nuevo, incluso con igual contenido: no se deduplican pedidos legítimos por dirección/cantidad. No se crean registros de operación simulados.

La app deshabilita el envío mientras procesa. Tras intentar, conserva en memoria de la sesión la clave y el contenido; si el resultado es incierto, los campos quedan bloqueados y el botón reintenta el mismo pedido. Esto sobrevive al desmontaje de la vista por falta de conexión y posterior consulta de cuenta. Una validación 400 permite corregir el borrador. Tras guardado confirmado, «Crear otra solicitud» obtiene otra clave. El borrador no se escribe en disco y se limpia al cerrar sesión; tras reiniciar la app, consultar Mis Solicitudes antes de iniciar otro pedido cuyo resultado anterior fuera incierto. No se guardan contraseñas ni se cambian las reglas existentes de token/HTTPS/servidor.

## Migración aplicada solo en LocalDB

`20261107191916_ClaveEnvioSolicitud` agrega:

- Columna opcional `SOLICITUDES_CLIENTE.ClaveEnvio`, SQL `uniqueidentifier NULL`.
- Índice `IX_SOLICITUDES_CLIENTE_ClaveEnvio`, único, filtrado por `[ClaveEnvio] IS NOT NULL`.

Permite múltiples solicitudes anteriores sin clave. Se conservaron **12 tablas de negocio**, 27 filas de catálogos y todos los datos: 6 usuarios, 5 solicitudes (las cinco sin clave), 5 servicios y 11 tareas. `__EFMigrationsHistory` es la tabla técnica de EF, no una tabla del negocio. No se modificaron cuentas ni contraseñas. No se ejecutaron scripts de preparación de datos: ya están disponibles las dos unidades necesarias.

El primer intento de actualización usó un binario anterior a la generación de la migración y EF lo rechazó por diferencias de modelo, antes de aplicar cambios. Se recompiló con la migración y se aplicó correctamente sin suprimir la comprobación. Se verificaron historial, índice y conteos.

**Pendiente para la publicación final:** revisar migraciones ya aplicadas en el hosting y aplicar esta migración aditiva junto con las anteriores que falten, antes de publicar la API que usa ClaveEnvio. No aplicar Down ni borrar bases. En este bloque no se contactó ni modificó MonsterASP.

## Prueba manual Windows

1. Abrir la solución en Visual Studio 2026. Iniciar la API actualizada con perfil HTTPS local. `ConexionApi.cs` conserva `UsarHosting = false`: Windows `https://localhost:7292/`, Android Debug `http://127.0.0.1:5148/` por USB. Abrir la app Windows Debug.
2. Entrar con **Cliente Prueba**, usando su contraseña local conocida. No cambiar cuentas, roles ni contraseñas. Solicitar nuevo servicio: dirección `PRUEBA LOCAL CLIENTE - Entrega 456`, cantidad **2**, tipo **Obra**, uso **Evento**, fecha deseada elegida. Probar primero dirección/fecha vacías y cantidad cero: deben impedir enviar. Completar y confirmar: debe mostrar número real y **Pendiente**. Anotar el número, sin asumir un ID fijo.
3. Abrir Mis Solicitudes: debe aparecer el pedido con sus datos, Pendiente y sin códigos de baño. Consultar de nuevo: una sola solicitud. Los pedidos anteriores de otros tipos del cliente también aparecen. Cerrar sesión.
4. Entrar como administrador → Solicitudes y Reclamos → número anotado. Debe mostrar el pedido real. Al preparar la guía estaban disponibles **PRUEBA-RETIRO-01 (ID 4)** y **PRUEBA-RETIRO-02 (ID 5)**, Apto/tipo Obra, sin asignación vigente ni mantenimiento bloqueante activo. Reutilizarlos si siguen disponibles; no resetear su historial ni cancelar trabajos anteriores para forzar disponibilidad. Elegir ambos, empleado activo **PRUEBA CUENTAS Windows**, frecuencia común y fecha de entregas. Confirmar: solicitud Aprobada, dos servicios Pendiente de entrega y dos tareas Entrega/Pendiente.
5. Volver a entrar como cliente → Mis Solicitudes: Aprobada, ambos códigos; nunca Resuelta por asignarlos. Mis Servicios muestra las dos nuevas asignaciones vigentes además de las anteriores.
6. Entrar como el empleado asignado → Tareas asignadas → cada entrega, identificada por baño/dirección/fecha → Registrar Trabajo → Actualizar Estado. Tras la primera, volver como cliente y actualizar Mis Solicitudes: debe seguir **Aprobada**. Completar la segunda y actualizar como cliente: debe mostrar **Resuelta**, y los dos servicios quedan Entregado.
7. Probar otra cuenta Cliente/Activa conocida: no debe ver este pedido. Probar actualizar sin API y recuperar: debe indicar error/sesión sin verificar, nunca «sin solicitudes» por un fallo. Los accesos administrativos no aparecen para el cliente.

### Resultado manual Windows: aprobado

El estudiante confirmó el recorrido completo: el cliente envió el pedido y lo vio **Pendiente**; el administrador asignó baños y entregas; el empleado completó las entregas; el cliente vio la solicitud **Resuelta**, los nuevos servicios y el contador actualizado.

Esta evidencia es **manual**, separada de las **166 pruebas automatizadas anteriores**. No se consideran confirmados por este mensaje los pasos específicos de validación de campos, reintentos, aislamiento con otra cuenta, desconexión/recuperación ni la consulta entre entregas parciales. Los datos y la disponibilidad indicados en la preparación corresponden al momento anterior a esta prueba: no se restablecen unidades ni trabajos para repetirla.

La integral manual Android queda para el final de los bloques contra API local (USB: `adb reverse tcp:5148 tcp:5148`); después, migraciones pendientes del hosting y republicación. `20261107191916_ClaveEnvioSolicitud` continúa **pendiente de aplicación al hosting**. Siguen pendientes vencimiento real del token y alternancia de servidores en dispositivo. Acciones del detalle del servicio (limpieza extra, dirección, reclamo y baja) siguen para sus bloques posteriores.

## Verificaciones automatizadas

Se agregaron **11 casos** en `SolicitudesClientePruebas.cs`: creación sin servicios/tareas, uso cruzado, integración completa con entregas parciales y resolución, reintento después de resolver, concurrencia sin duplicar, otra clave para pedido nuevo, clave ajena/alterada y detalle ajeno protegidos, validaciones, otros tipos/estados sin clave, seis situaciones de permisos y borrador de app con clave estable/nueva. Se conservan las 155 pruebas anteriores. Primera compilación de pruebas requirió corregir un alias de namespace; no llegó a ejecutar casos. La ejecución focalizada posterior aprobó **11/11**. Una única suite completa de cierre: **166 correctos, 0 fallidos, 0 omitidos**. Total **177 ejecuciones de casos**, 166 casos distintos. Sin nuevas pruebas visuales simples ni repetición por documentación.

API y Razor enlazados compilaron con las pruebas. App Debug Windows y Android: **0 errores y 0 advertencias**, con los parámetros temporales Java ya documentados, sin cambios de herramientas. La ejecución manual Windows confirmada arriba es independiente de TestServer y sus bases aisladas. Revisión de diff, 177 archivos candidatos e índice vacío: sin generados, perfiles privados ni indicadores de secretos en las rutas/textos examinados. PDF y PNG originales conservan sus hashes. Este cierre solo actualizó documentación: no se repitieron pruebas ni compilaciones. **Bloque cerrado y listo para commit**, sin comenzar el siguiente, commit, push, merge ni cambios en hosting. Mensaje sugerido: `feat: solicitar nuevos servicios y consultar solicitudes del cliente`.
