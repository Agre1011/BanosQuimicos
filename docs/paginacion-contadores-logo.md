# Contadores, listados paginados y logo Celeris

Rama `feature/paginacion-tareas-logo`, desde master limpio `30dcbf2`. API local/LocalDB, sin migraciones ni cambios en datos del estudiante o hosting. Se mantienen reglas de estados, propiedad, permisos y cumplimiento. Solo cambia el conteo de trabajo sin finalizar y la forma de consultar/presentar listados.

## Contadores y filtros

El inicio del empleado muestra **tareas sin finalizar para hoy**, según la fecha programada y el día de Argentina (UTC−3). Incluye Pendiente y En curso, aunque el trabajo ya esté registrado. Excluye Realizada y Cancelada. Registrar Trabajo no descuenta; Actualizar Estado, después de finalizar correctamente, sí. Al volver desde las tareas al inicio se crea otra vez InicioEmpleado y consulta el resumen actual de la API, sin sumar una página ni reutilizar el número anterior. Sin conexión no inventa un cero.

El listado del empleado incluye todas las fechas, no solo hoy: **Sin realizar** es el filtro inicial (Pendiente/En curso); **Realizadas** corresponde al estado interno Realizada; **Todas** incluye también Cancelada, identificada por su estado real. El filtro se aplica en SQL antes de paginar. Ir al detalle y volver conserva filtro/página. Si finalizar una tarea elimina la última página del filtro Sin realizar, la API devuelve la última válida. Cambiar filtro vuelve a página 1; Actualizar consulta conserva la selección.

El contador de servicios del cliente sigue contando todos sus servicios vigentes (Pendiente de entrega/Entregado), no los diez que se muestran en Mis Servicios. No se cambia la definición de vigencia.

## Paginación y contratos

Los seis listados consultan páginas de **10 elementos**, con total, página y Anterior/Siguiente compartidos mediante Paginador. El total 0 corresponde a página 1 y controles deshabilitados, con mensaje de vacío/sin coincidencias. Un error no equivale a lista vacía ni total 0 válido. Gestionar cuentas conserva su paginación existente.

Rutas nuevas usadas por la app:

- Empleado: `GET /api/tareas/mias/pagina?pagina=1&estado=Sin%20realizar`.
- Estado Baños: `GET /api/banos/estado/pagina?pagina=1&inventario=Activos&condicion=Todos`.
- Baños y Servicios: `GET /api/servicios/pagina?pagina=1&tipoBano=Todos` (Todos/Obra/Evento; tipo físico, no uso).
- Solicitudes y Reclamos del administrador: `GET /api/solicitudes/pagina?pagina=1`.
- Mis Servicios: `GET /api/servicios-cliente/mios/pagina?pagina=1`.
- Mis Solicitudes: `GET /api/solicitudes-cliente/mias/pagina?pagina=1`.

Respuesta: `elementos`, `pagina`, `tamanoPagina` (siempre 10) y `total`. Página solicitada menor que 1 o filtros no admitidos: 400. Una página superior a la última se ajusta sin desbordar el desplazamiento. Sin coincidencias: total 0, elementos vacíos. Propietario, filtros y orden estable se aplican en la consulta de EF antes de Count/Skip/Take. Los órdenes existentes se conservan con ID de desempate: tareas por programación (sin fecha al final)/ID; baños por código/ID; servicios administrativos por código/ID descendente; solicitudes por fecha descendente/ID descendente; servicios del cliente por código/ID. Los filtros inventario/condición se combinan antes de paginar; Apto no significa Disponible.

Las rutas previas sin `/pagina` se conservan por compatibilidad de contratos y pruebas existentes; las seis pantallas ya usan las paginadas. No se pagina en la app sobre una lista descargada completa. Los detalles, formularios y desplegables de empleados/tipos/baños seleccionables conservan sus consultas, sin paginación añadida. Los resúmenes tienen consultas independientes sobre el total correspondiente. No se modifican autenticación ni envío de tokens.

## Recurso gráfico

`BanosQuimicos.App/wwwroot/images/celeris.png` es una copia exacta del adjunto, con fondo original, sin recortar ni transformar. SHA-256: `409669BDCA770650F43BF8A4FD49E41B70E050EEFCF357B279D576030C9E9B46`.

MarcaCeleris reemplaza el cuadrado BQ **únicamente** en login, registro e inicios de Cliente, Administrador y Empleado. El encabezado no se muestra en listados, detalles, formularios operativos, gestión de cuentas, espera ni errores de sesión; sus títulos y navegación se conservan sin volver a colocar BQ. Imagen local, `alt="Celeris"`, ancho adaptable con máximo de 22rem, altura automática y proporción original 443×204. Se conservan títulos, nombre/rol y textos útiles. No se desactiva ni elimina el fondo de la imagen. No se agregan pantallas.

## Guía Windows — comprobaciones aprobadas según el cierre

Usar API local con perfil https y app Windows en Visual Studio 2026. ConexionApi conserva UsarHosting=false (https://localhost:7292). No usar MonsterASP, cambiar credenciales/roles anteriores ni reiniciar trabajos finalizados. Para navegar a una segunda página hacen falta más de 10 coincidencias; si no existen en los datos locales, Siguiente debe estar deshabilitado. Los escenarios con 11/12/23 registros se verifican automáticamente en bases aisladas, no se llenó la base del estudiante con datos artificiales.

### Grupo 1: logo y pantallas (3 comprobaciones)

1. Abrir login y Crear cuenta, volver sin registrar: debe verse Celeris con su fondo, sin el cuadrado BQ y conservando títulos/campos.
2. Ingresar con los roles locales conocidos: cada inicio conserva nombre, rol y acciones, y muestra el mismo logo. No compartir credenciales en la guía ni capturas.
3. Reducir/ampliar ventana: logo completo y proporcionado, sin estiramiento ni recorte; comprobar desplazamiento y tabulación.

### Grupo 2: contador del empleado (3 comprobaciones)

1. Disponer de una tarea local de prueba programada para hoy y asignada al empleado; si hace falta, crear una nueva por el recorrido administrativo normal, sin modificar trabajos anteriores. Ingresar como ese empleado y anotar el número de tareas sin finalizar de hoy.
2. Abrir esa tarea, Registrar Trabajo y volver al inicio: el número debe mantenerse. Las tareas de otras fechas/empleados no deben sumarse a ese indicador.
3. Reabrir, Actualizar Estado y volver al inicio: debe disminuir en uno si correspondía a hoy. Si no había más trabajo de hoy, debe mostrar 0. Realizadas/Canceladas no se cuentan. No confundir registro con finalización.

### Grupo 3: tareas y regreso del detalle (3 comprobaciones)

1. Entrar a Tareas asignadas: Sin realizar inicialmente, hasta diez tarjetas Pendiente/En curso, total y página. Revisar Realizadas y Todas, con estados identificados y mensaje claro si no hay coincidencias.
2. Con más de diez resultados, ir a página 2, abrir detalle y volver: debe conservar filtro/página. Cambiar filtro debe ir a página 1. Actualizar consulta debe mantener el filtro.
3. Si hay exactamente una tarea en la última página de Sin realizar, finalizarla y volver: debe ajustar a la última página válida. No eliminar ni resetear trabajos para forzar ese escenario; si no se dispone de ese conjunto, conservarlo como verificado automáticamente.

### Grupo 4: listados administrativos (3 comprobaciones)

1. Estado Baños: combinar filtros de inventario y condición, navegar si hay más de diez, actualizar manteniendo selección; al cambiar cualquiera de los filtros debe volver a página 1. Apto asignado debe seguir entre Aptos aunque no esté disponible.
2. Baños y Servicios: máximo diez tarjetas por página, total real, orden estable y extremos de navegación deshabilitados. Los formularios y selección de empleados mantienen su funcionamiento y no tienen paginación nueva.
3. Solicitudes y Reclamos: abrir un detalle desde otra página y volver; conservar página y datos reales. Con cero coincidencias/registros mostrar vacío y no confundirlo con error.

### Grupo 5: cliente y conectividad (3 comprobaciones)

1. Mis Servicios: total/páginas, detalle y regreso a la misma página. Si hay más de diez servicios vigentes, el contador del inicio debe mostrar el total, no 10. Con otro cliente vacío no deben aparecer datos anteriores.
2. Mis Solicitudes: revisar Anterior/Siguiente, total y estados/respuestas existentes; Actualizar consulta conserva la página o la ajusta si corresponde. No se agregan edición, cancelación ni eliminación.
3. Detener y reiniciar la API local para comprobar error y recuperación con las opciones de consulta. No debe mostrarse un contador cero o un listado vacío como si la consulta fallida fuera válida.

## Evidencias y pendientes

Resultados efectivos de pruebas y compilación: sección 31 del plan. Las verificaciones automáticas no acreditan apariencia visual ni ejecución en un teléfono. Comprobaciones Windows confirmadas según la sección siguiente; ajustes de filtro de tipo físico y ubicación del logo aprobados según el cierre. Prueba integral Android contra API local al terminar los bloques, vencimiento real del token y alternancia de servidores siguen pendientes. Después se harán correcciones, migraciones pendientes y republicación. Este bloque no agrega migración ni cambia el hosting. Sin commit, push o merge.


Evidencia final: **10 casos nuevos; 107 casos distintos afectados verificados**. Primera corrida106 correctos y1 expectativa antigua del mensaje vacío; tras corregirla y ajustar/ampliar dos casos relacionados, **3/3 focalizados correctos**, sin repetir el resto. 110 ejecuciones acumuladas; **no se ejecutó la suite completa** ni se afirma que los240 casos definidos hayan corrido. API/Razor y app Debug Windows/Android compilaron correctamente,0 errores/advertencias. Los manifiestos de recursos de ambos destinos incluyen el PNG. La comprobación visual/manual de esta guía sigue pendiente; no se confunde compilación con prueba en Android.


## Confirmaciones manuales Windows y ajustes posteriores — 08/11/2026

El estudiante confirmó **contador y filtro de tareas: funcionan**, y **paginación y regreso del detalle: funcionan**. El logo se ve bien en Windows, pero aparecía en más pantallas de las solicitadas. Se conserva esa observación; no se marca como aprobada la nueva restricción de ubicación del logo ni el filtro de tipo físico. No se atribuyen otras pruebas manuales no informadas. Estas evidencias son independientes de las pruebas automatizadas previas.

### Ajuste A — filtro aprobado en Windows; pasos para repetir

1. Como administrador, elegir **Todos / Baños de obra / Baños de evento**. Verificar Tipo de baño, no Tipo de uso: un baño Obra usado para Evento debe figurar entre Baños de obra. La API filtra mediante la relación con TIPOS_BANO antes de contar/paginar; no usa prefijos de códigos ni nombres de datos de prueba.
2. Si hay más de diez resultados, ir a otra página, abrir/cerrar el selector o detalle existente y actualizar: conserva filtro/página. Cambiar el tipo vuelve a página1. Una combinación sin servicios muestra «No hay servicios que coincidan con el tipo de baño seleccionado». No se borran registros para probarlo.
3. Las altas de baños de la app funcionan con el filtro al tener un servicio asociado. Un baño recién creado **sin servicio** se consulta en Estado Baños; este ajuste no convierte Baños y Servicios en otro inventario ni crea servicios ficticios para mostrarlo. Si no hay datos de un tipo, el vacío es válido.

### Ajuste B — alcance aprobado en Windows; pasos para repetir

1. Login, registro e inicio del administrador, empleado y cliente deben conservar Celeris, su fondo y proporciones.
2. Abrir listados, detalles, formularios operativos y Gestionar cuentas: no deben mostrar Celeris ni el cuadrado BQ, pero sí sus títulos y navegación.
3. Volver al inicio: reaparece el logo solo allí. Verificar ventana reducida y navegación normal sin alterar la imagen.

**Verificación automatizada del ajuste:** 2 casos nuevos focalizados correctos; API/Razor y app Debug Windows/Android compilaron con **0 errores y 0 advertencias**. Uno comprueba tipo físico con usos cruzados, 24 altas mediante el endpoint real de la app más un código histórico sin prefijo y paginación/selección del componente; otro comprueba la lógica de visibilidad del encabezado en acceso, roles y pantallas hijas. No es una revisión visual Android. No se repitieron los107 casos previos ni la suite completa. Sin migraciones ni cambios en datos locales existentes/hosting. **Ajustes aprobados posteriormente según el cierre**, sin commit, push o merge.


## Cierre final: aprobaciones manuales Windows — 08/11/2026

Se consolidan exclusivamente las comprobaciones informadas por el estudiante como **aprobadas manualmente en Windows**:

- Contador y filtro de tareas.
- Paginación y regreso del detalle.
- Filtro **Todos / Baños de obra / Baños de evento**.
- Logo únicamente en login, registro y los tres inicios, sin Celeris ni BQ en listados y detalles.

La observación previa de logo en pantallas extra queda corregida y aprobada por esta confirmación. No se atribuyen comprobaciones manuales adicionales no informadas. Los pasos de la guía permanecen para repetir el recorrido, sin afirmar que cada subcaso opcional se haya probado manualmente.

Estas aprobaciones se separan de la evidencia automatizada previa: **107 casos afectados verificados** tras la corrección focalizada de3 casos, más **2/2 regresiones de los ajustes**. Hay12 casos nuevos en el bloque y242 definidos en el proyecto; **no se ejecutó la suite completa** ni se afirma una ejecución completa de242. La compilación final anterior de API/Razor y app Debug Windows/Android fue correcta, **0 errores y 0 advertencias**. No se confunde compilación con revisión visual Android.

**Bloque feature/paginacion-tareas-logo cerrado y listo para commit.** En este cierre solo se actualizan plan y guía: no se repiten pruebas ni compilaciones ni se cambian código, datos o configuración. Sin migración nueva; las migraciones pendientes para hosting mantienen su estado. Android integral contra API local, correcciones y posterior migraciones/republicación/comprobación de hosting continúan para el final; vencimiento real del token y alternancia de servidores siguen pendientes generales.

Título de commit y PR sugerido: **feat: paginar listados y actualizar tareas pendientes con logo Celeris**.

Descripción breve del PR: «Corrige el contador de tareas sin finalizar de hoy e incorpora filtros y paginación de diez elementos en la API, conservando página y selección al regresar. Agrega filtro por tipo físico en Baños y Servicios y limita Celeris a login, registro y los tres inicios. Conserva permisos y datos, sin migraciones. Verificado con pruebas focalizadas, compilación Windows/Android y las comprobaciones manuales Windows confirmadas; Android integral y republicación pendientes.»

Sin commit, push, merge ni cambios en hosting. Esperar confirmación antes de comenzar otro bloque.
