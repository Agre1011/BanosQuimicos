# Plan de trabajo

Fecha de revisión: 27/10/2026. Bloque 0 cerrado. Bloque 1 autorizado posteriormente por el estudiante con alcance limitado a estructura y compilación; ver sección 7. El resto del orden es una propuesta pendiente de confirmación.

## 1. Fuentes revisadas y límites de lectura

Se revisaron los tres originales existentes en `docs`. No existían AGENTS.md ni este plan antes de crearlos.

| Fuente | Lectura realizada |
| --- | --- |
| Sistema de Gestión Móvil para Empresa de Baños Químicos (3).pdf | Siete páginas: texto extraído y revisión visual de todas. Páginas 1-2: idea, alcance y descripción funcional legibles. Página 3: ER; página 4: UML; páginas 5-7: capturas de Figma de acceso/empleado, administrador y cliente. Se ampliaron las páginas de pantallas. |
| Imagen de ChatGPT 27 oct 2026, 09_11_39 a.m.png | ER independiente legible: tablas, campos principales y relaciones. Misma estructura general que el ER del PDF, con distinta disposición. |
| Imagen de ChatGPT 27 oct 2026, 09_11_49 a.m.png | UML independiente legible: actores, 19 casos de uso y conexiones principales, coincidentes en contenido general con el PDF. |

Las capturas permiten reconocer pantallas, acciones y navegación, pero algunos textos pequeños, valores de ejemplo y símbolos de relaciones del ER incrustado tienen legibilidad limitada. No se consideran validados todos los detalles de cardinalidad ni cada texto pequeño. Ampliar una captura no recupera detalle perdido. No hay acceso al archivo Figma editable, menús desplegados, validaciones ni flujos que no estén capturados. No se deducen de las flechas reglas de autorización o transiciones completas.

Figma visible: login, registro, inicio de cada rol, tareas y detalle de empleado; clientes/empleados, baños/servicios, estado de baños y gestión de solicitudes del administrador; servicios y detalle, solicitudes, nuevo servicio, cambio de dirección y reclamo del cliente. El registro muestra nombre, apellido, correo, teléfono, contraseña, razón social, CUIT y dirección principal. Su botón dice «Iniciar Sesión», aunque es un formulario de registro. Empleado tiene «Registrar Trabajo» y «Actualizar Estado», sin formularios de destino visibles para esos botones.

ER observado (12 tablas): USUARIOS, ROLES, CLIENTES, BANOS_QUIMICOS, TIPOS_BANO, ESTADOS_BANO, SERVICIOS, UBICACIONES_SERVICIO, SOLICITUDES_CLIENTE, TAREAS_OPERATIVAS, TIPOS_OPERACION y ESTADOS_OPERACION. El empleado se referencia mediante USUARIOS; no hay tabla EMPLEADOS separada. SERVICIOS referencia un cliente y un baño; las ubicaciones incluyen fechas y EsActual. Solicitudes pueden tener servicio opcional; tareas pueden referenciar solicitud opcional. USUARIOS contiene estado de cuenta, motivo de suspensión y fecha de baja. Esto describe el dibujo, no aprueba todavía un esquema de implementación.

## 2. Requisitos aprobados documentados

La aplicación centraliza alquileres y seguimiento operativo: servicios, baños asignados, ubicaciones, entregas, retiros, limpiezas, mantenimiento, solicitudes y reclamos. App móvil conectada a API REST y base relacional. Pagos y facturación quedan fuera.

| Rol | Funciones según texto aprobado (PDF, páginas 1-2) |
| --- | --- |
| Administrador | Gestionar usuarios, clientes, empleados, baños, servicios activos, ubicaciones, solicitudes, reclamos y tareas; consultar estados de baños. Revisar cuentas, activar, suspender, eliminar lógicamente y cambiar rol. Revisar reclamos y generar tareas cuando corresponda. UML explicita asignar tareas. |
| Empleado | Consultar tareas asignadas de entrega, retiro, limpieza semanal/extra y mantenimiento. Marcar realizadas, agregar observaciones y registrar reposición de papel higiénico. |
| Cliente | Registrarse e iniciar sesión; consultar sus baños y direcciones de servicios; solicitar limpieza extra, informar traslado ya realizado o pedir traslado, reclamar, solicitar baño adicional, pedir baja del servicio y consultar estados de sus solicitudes. |

Reglas explícitas:

- El registro crea un cliente por defecto; el administrador puede cambiar su rol.
- Un cliente puede tener varios baños simultáneos, incluso en diferentes direcciones.
- Cada servicio registra baño, ubicación, tipo de uso y frecuencia de limpieza.
- Clasificación principal obra/evento, permitiendo uso cruzado. Los códigos B.O-01 a B.O-40 y B.E-01 a B.E-50 describen el inventario del texto; no se asumen límites rígidos de software.
- Las limpiezas de obra suelen ser semanales, principalmente lunes y martes. No es una prohibición de otros días. En eventos generalmente no hay limpieza durante el uso; no se interpreta como prohibición absoluta.
- Limpiezas extra y reclamos pueden generar tareas; el documento no exige generación automática para toda solicitud.
- Eliminar una cuenta es una baja lógica, sin borrar físicamente su registro.

Tecnologías previstas por indicación del estudiante: C#/.NET 10, MAUI Blazor Hybrid/Razor, Web API con controladores, SQL Server/EF Core y Visual Studio 2026; pruebas en Android. No se eligieron paquetes ni versiones de EF Core todavía.

## 3. Diferencias y decisiones pendientes

Ninguna observación de esta sección cambia por sí misma la documentación aprobada.

| ID | Evidencia o diferencia | Decisión que falta |
| --- | --- | --- |
| D1 | UML caso 9 y Figma incluyen solicitar nuevo servicio; el texto enumera baño adicional. Figma pide cantidad, tipo, dirección y fecha. ER almacena un baño por servicio y la solicitud no muestra campos específicos de cantidad/tipo/fecha deseada. | Confirmar flujo de nuevo servicio frente a adicional y cómo representar un pedido de varios baños. |
| D2 | UML dice que la cuenta queda pendiente de gestión; texto contempla activarla. Registro de Figma parece conducir a inicio y su botón dice Iniciar Sesión. Login pide nombre de usuario, pero registro no muestra ese campo. | Definir estado inicial, acceso mientras está pendiente, identificador de login y alta del primer administrador. |
| D3 | Texto permite informar un traslado ya realizado o pedir que la empresa lo haga; Figma muestra ambas opciones. ER tiene NuevaDireccion e historial de ubicaciones, pero no un campo explícito que distinga ambas modalidades. | Definir quién valida, cuándo cambia la ubicación actual y cuándo se genera tarea; conservar historia. |
| D4 | UML distingue nuevo servicio y baño adicional; Figma no muestra una pantalla separada para adicional. No se ven formularios completos de altas/ediciones administrativas ni de registro de trabajo/reposición de papel. | Acordar las pantallas faltantes con el estudiante/profesor antes de implementarlas; no quitar funciones escritas por faltar una pantalla. |
| D5 | Pantalla de baños mezcla condición física (buen/mal estado) con disponibilidad/asignación/mantenimiento; ER tiene un catálogo ESTADOS_BANO. | Acordar si son dimensiones separadas o un único estado y sus transiciones. |
| D6 | Solicitudes y tareas comparten TIPOS_OPERACION y ESTADOS_OPERACION en ER. UML y pantallas no definen catálogos ni todas las transiciones. | Definir estados, tipos válidos por entidad, rechazo/cancelación, quién cambia cada estado y si una solicitud genera varias tareas. |
| D7 | Pantalla de servicio muestra empleado; ER asigna empleado a tarea. Texto no establece empleado fijo por servicio ni generación automática semanal. | Confirmar origen de ese dato y planificación manual o automática de tareas recurrentes. |
| D8 | Figma ejemplifica limpieza 2 veces por semana y código B.O-51; texto dice «suelen» semanalmente y ejemplifica B.O hasta 40. | Confirmar frecuencia configurable y códigos permitidos; no tratar ejemplos como restricciones. |
| D9 | ER llama Contrasena al campo de usuario. | Propuesta técnica: almacenar únicamente un hash seguro mediante mecanismos estándar de ASP.NET Core; nunca contraseña en texto plano. Elegir mecanismo de autenticación antes del bloque de acceso. |
| D10 | Texto indica pedir baja; Figma dice Dar de Baja. No se explican retiro, fin del servicio ni liberación del baño. | Confirmar aprobación administrativa y momento de los cambios; no dar de baja inmediatamente por el texto del botón. |
| D11 | Un rol por usuario en ER; perfil CLIENTES opcional y empleado representado por usuario. Texto permite cambiar rol. | Definir qué ocurre con servicios y tareas existentes al cambiar rol, suspender o dar de baja. Validar relaciones antes de migraciones. |

Actualización posterior: D1, D5 y D6 tienen decisiones confirmadas por el estudiante en la sección 9; también se confirmaron partes de D2/D3/D9. La tabla anterior conserva las observaciones originales para explicar las diferencias; consultar C1-C6 para distinguir lo resuelto de los detalles aún pendientes.

## 4. Entorno observado (solo consultas)

| Consulta | Resultado |
| --- | --- |
| dotnet --list-sdks | 7.0.402, 8.0.403 y 10.0.401 en C:\Program Files\dotnet\sdk. |
| dotnet --info | SDK seleccionado 10.0.401, MSBuild 18.9.11; Windows x64; runtime .NET y ASP.NET Core 10.0.12. Sin global.json. |
| dotnet workload list | android 36.1.69; ios y maccatalyst 26.5.10301; maui-windows 10.0.20. Origen VS 18.10.12224.181. Workload version 10.0.400-manifests.b0700452; informa que no hay workload sets instalados. No se ejecutó restore. |
| Inspección de packs instalados | Existen Microsoft.Maui.Sdk, Microsoft.Maui.Resizetizer.Sdk y packs Microsoft.Android. No se afirma que exista un workload maui-android por no aparecer con ese identificador en la consulta. |
| dotnet new list maui | Disponibles maui-blazor y otras plantillas MAUI. No se ejecutó ninguna plantilla. |
| vswhere -products * -format json | Visual Studio Community 2026, versión 18.10.3 (instalación 18.10.12224.181), completa y ejecutable según registro. No se abrió el IDE. |
| git --version / git status --short | Git 2.49.0.windows.1 disponible. La carpeta no es un repositorio Git. No se ejecutó git init ni commit. |

Las primeras consultas de --info/workloads fallaron por acceso denegado al administrador de servicios dentro del aislamiento; se repitieron con autorización fuera del aislamiento y finalizaron correctamente. La consulta de plantillas también requirió autorización por acceso a la caché de plantillas. No se instaló ni actualizó nada.

Pendiente: comprobar JDK/SDK Android, dispositivo o emulador y conexión, restauración/compilación real de MAUI Android, depuración en VS y conectividad API desde Android. SQL Server no fue consultado ni configurado. Las herramientas encontradas no prueban una compilación satisfactoria. No se ejecutaron pruebas de aplicación porque todavía no existe.

## 5. Bloques propuestos y criterios de cierre

Antes de cada bloque: explicar objetivo y archivos previstos, resolver sus decisiones necesarias y esperar confirmación. Al cerrar: explicar cambios, archivos, pasos reproducibles, verificaciones realizadas y pendientes, declarar cierre y proponer commit sin ejecutarlo automáticamente.

| Bloque | Objetivo y alcance | Criterio para comprobarlo | Decisiones previas |
| --- | --- | --- | --- |
| 0. Preparación (cerrado) | Revisar fuentes/entorno y crear instrucciones y este plan. | Solo dos archivos nuevos en el proyecto, originales conservados; revisión visual y límites registrados. | Confirmación para avanzar. |
| 1. Base técnica mínima | Crear solución sencilla con app MAUI Blazor Hybrid y API con controladores; navegación inicial de referencia y conexión mínima sin reglas de negocio. | Abrir en VS 2026, compilar API y app Android, iniciar app en dispositivo/emulador y comprobar una respuesta de API. Explicar recorrido Razor -> HTTP -> controlador. | Confirmar estructura y nombre, dispositivo/emulador y entorno Android; endpoint local accesible. Si hacen falta instalaciones, consultarlas. |
| 2. Registro, acceso y cuentas | Primer bloque funcional: registro como cliente, login, permisos y administración de cuentas; persistencia mínima con EF Core/SQL Server. | Registro no permite elegir rol privilegiado; acceso según estado acordado; llamadas directas a API respetan permisos; baja lógica conserva registros; contraseñas no se almacenan en claro. Probar roles y acceso denegado. | D2, D9, D11; instancia SQL Server y autenticación local; revisar modelo mínimo antes de autorizar migraciones. |
| 3. Baños y servicios | Inventario, cliente, asignación, tipo de uso, frecuencia y ubicación, consulta de servicios propios. | Cliente con varios baños/direcciones; uso cruzado obra/evento; aislamiento entre clientes; probar disponibilidad según reglas acordadas. | D1 en cuanto a servicio por baño, D5, D8; confirmar solapamientos y cardinalidades. |
| 4. Solicitudes y reclamos | Limpieza extra, dirección, adicional/nuevo servicio si se confirma, baja y seguimiento por cliente; revisión administrativa. | Cada pedido queda asociado al cliente correcto y al servicio cuando corresponda; solo dueño/administrador acceden; estados y cambios de dirección siguen el acuerdo. | D1, D3, D4, D6, D10; acordar datos de formularios faltantes. |
| 5. Tareas operativas | Asignar tareas y registrar entrega, retiro, limpieza, mantenimiento, observaciones y papel repuesto. | Empleado ve y completa sus tareas; otro empleado no puede modificarlas; resultado y fecha quedan guardados; evitar duplicación al repetir una finalización. | D4, D6, D7; efectos de cada tarea sobre servicio y baño. |
| 6. Validación y defensa | Recorrido completo en Android, ajustes de Figma y explicación de arquitectura/reglas. | Demostrar registro -> gestión -> servicio -> solicitud -> tarea -> consulta; probar permisos, persistencia y errores de conexión. Registrar resultados reales y pendientes; estudiante puede explicar cada capa. | Datos ficticios y guion de defensa; confirmar criterios del profesor. |

Propuesta de estructura: dos proyectos principales (app y API), con carpetas sencillas; evaluar un proyecto de contratos compartidos solo si se justifica. SQL Server se usa desde la API, no directamente desde el teléfono. No se propone añadir aplicación web, pagos, facturación, mapas, notificaciones push, optimización de rutas ni modo sin conexión. Nada de ello está aprobado por este plan.

## 6. Verificación de esta entrega y próxima confirmación

Realizado: inventario previo; comprobación de inexistencia de los archivos destino; extracción de texto y lectura visual de siete páginas y dos PNG; consultas de entorno registradas; creación exclusiva de AGENTS.md y docs/plan-trabajo.md en el proyecto; lectura final de ambos y comparación SHA-256 de los tres originales. Las imágenes temporales de revisión quedaron fuera del proyecto, en el área de visualizaciones de Codex.

Pendiente: todas las pruebas de compilación, ejecución, base de datos, API y Android. No hay solución, proyectos, base creada, migraciones, repositorio inicializado, commits ni publicaciones realizados por esta tarea.

Para comenzar el bloque 1 alcanza con confirmar: (1) autorizar ese bloque y la estructura app/API; nombre propuesto BanosQuimicos; (2) elegir teléfono Android físico o emulador. D1-D11 se conservan como preguntas para resolver antes del bloque que dependa de ellas; no es necesario decidirlas todas ahora.

Mensaje de commit sugerido para cuando se autorice inicializar Git y versionar: `docs: preparar instrucciones y plan del proyecto`. No ejecutado.

## 7. Bloque 1: estructura inicial

El estudiante confirmó crear BanosQuimicos.Api y BanosQuimicos.App en .NET 10, inicializar Git local y compilar. Postergó elegir dispositivo Android; no autorizó entidades, autenticación, base, migraciones, commits ni publicación. Esta confirmación reemplaza para este bloque el criterio original de ejecutar Android y conectar app/API: la app todavía no consume la API. Las secciones anteriores registran la revisión inicial.

Cambios:

- `BanosQuimicos.sln`: incluye ambos proyectos.
- `BanosQuimicos.Api`: arranque con controladores y `Controllers/EstadoController.cs`, GET `/api/estado`; archivo `.http` para probarlo y perfil HTTP local en puerto 5148. Se retiraron ejemplos WeatherForecast y OpenAPI para mantener la base mínima.
- `BanosQuimicos.App`: plantilla MAUI Blazor Hybrid; pantalla `Components/Pages/Home.razor` con nombre del sistema y layout simple. Se retiraron páginas de clima/contador y menú de ejemplo. Destinos net10.0-android y net10.0-windows10.0.19041.0; no se habilitan iOS/MacCatalyst.
- `.gitignore`: plantilla .NET/Visual Studio. Git inicializado localmente; sin commits ni remotos.
- `README.md`: instrucciones para abrir, compilar y ejecutar cada proyecto en Visual Studio 2026.

Verificaciones realizadas:

- `dotnet sln BanosQuimicos.sln list`: los dos proyectos incluidos.
- `dotnet build BanosQuimicos.sln`: finalizó correctamente, cero errores y cero advertencias. Compiló API net10.0 y app Android/Windows. Se restauraron paquetes NuGet; no se instalaron workloads ni SDK nuevos.
- La compilación aislada falló sin diagnóstico; se repitió con autorización fuera del aislamiento.
- API iniciada con perfil HTTP y registro de escucha en localhost:5148. La primera ejecución aislada no fue accesible desde las consultas HTTP; se detuvo y se repitió fuera del aislamiento.
- Petición HTTP real a `/api/estado`: HTTP 200 y JSON `{"mensaje":"La API de Baños Químicos está funcionando."}`, comprobados automáticamente. Se detuvo el servidor de prueba al terminar.
- `.gitignore` comprobado para bin, obj y .vs; los originales PDF/PNG no están ignorados y sus hashes SHA-256 coinciden con la revisión inicial.

Pruebas confirmadas por el estudiante después del cierre inicial: ejecutó la app desde Visual Studio en Windows y en un teléfono Android físico conectado por USB. En ambos abre y muestra la pantalla inicial. Son pruebas informadas por el estudiante, no ejecuciones realizadas por el agente.

Pendiente visual V1: en Android el título queda parcialmente tapado por la franja superior. Registrar para una corrección posterior; por indicación del estudiante no se corrige en esta revisión. No se atribuye todavía una causa técnica comprobada.

Pruebas pendientes: conexión entre app y API (no implementada ni probada). No se comenzó el bloque 2.

Cómo repetir las comprobaciones: seguir `README.md`. Para el endpoint iniciar API con perfil http y consultar http://localhost:5148/api/estado; respuesta esperada HTTP 200 con propiedad mensaje. Para Android primero pedir confirmación del dispositivo al estudiante.

Mensaje de commit sugerido: `chore: crear estructura inicial de API y app MAUI Blazor`. No ejecutado.

Bloque 1 cerrado dentro del alcance confirmado. El estudiante ya confirmó y probó teléfono físico por USB. Esperar nueva confirmación antes de continuar.

## 8. Revisión previa al primer commit

Se revisaron `git status`, el índice (vacío), los archivos candidatos mediante `git ls-files --others --exclude-standard` y las reglas de `.gitignore`. Se conservan solución, proyectos, código, recursos MAUI/web, instrucciones, README y documentación original. Los archivos personales `.csproj.user`, `.vs`, `bin` y `obj` están ignorados. Se añadieron exclusiones explícitas para APK/AAB fuera de las carpetas de compilación, claves locales, variantes privadas de .env y temporales.

Se revisaron los archivos de configuración y perfiles de ejecución: contienen logging y URLs locales, sin credenciales. La búsqueda de indicadores de secretos en los textos candidatos no detectó secretos; las coincidencias fueron comentarios/instrucciones y marcadores de la plantilla del manifiesto Windows. Los PDF/PNG originales se conservan; esta búsqueda textual no constituye un análisis automático de secretos dentro de imágenes.

La simulación `git add --dry-run` fue bloqueada por permisos del aislamiento al intentar crear `.git/index.lock`; no agregó archivos. La comprobación de candidatos se hizo con consultas de solo lectura de Git, sin preparar archivos para commit.

Resultado: archivos listos para el primer commit de estructura inicial, con V1 registrado como pendiente conocido. No se ejecutaron commits ni push, no se corrigió la interfaz y no se inició otro bloque. No se repitió la compilación porque solo cambiaron exclusiones y documentación.

## 9. Preparación del modelo: decisiones confirmadas por el estudiante

Esta sección complementa el diagrama original aprobado por el profesor; no lo reemplaza ni afirma que el profesor ya haya validado estos ajustes. Se conservan sin modificar el PDF y las dos imágenes originales. El estudiante informó que realizó el primer commit y push. Continúa autorizada únicamente la preparación documental del modelo, sin entidades, bases ni migraciones.

### C1. Solicitudes de varios baños y baño adicional (D1)

Confirmado:

- Un servicio corresponde a un baño.
- Una solicitud de varios baños puede originar varios servicios.
- Un baño adicional origina un servicio nuevo, conservando la solicitud de origen y el servicio de referencia.
- En esta primera versión cada pedido tiene un solo tipo de baño solicitado. El tipo solicitado no elimina la regla aprobada de uso cruzado obra/evento.

Ejemplo: una solicitud de tres baños origina tres servicios individuales. Un pedido posterior de un baño adicional origina un cuarto servicio y conserva el vínculo con su solicitud y con el servicio de referencia.

Diferencia respecto del ER original: SERVICIOS ya referencia un baño, pero SOLICITUDES_CLIENTE no muestra cantidad, tipo solicitado ni fecha deseada, y no existe vínculo explícito entre la solicitud y todos los servicios que origina.

Ajuste estructural propuesto para expresar la decisión conservando tablas: agregar en SOLICITUDES_CLIENTE cantidad y tipo de baño solicitado, dirección de entrega y fecha deseada para esos pedidos; usar su referencia opcional a servicio como servicio de referencia/contexto. Agregar en SERVICIOS una referencia opcional a la solicitud de origen. Así se distinguen las dos direcciones: servicio existente que motiva un pedido y servicios nuevos resultantes. Los nombres, obligatoriedad por tipo y demás detalles de campos son una propuesta técnica pendiente de revisión; las relaciones y reglas de negocio anteriores sí están confirmadas. No crear una tabla de detalle de pedido mientras un solo tipo por solicitud alcance.

### C2. Mantenimiento sin servicio activo

Confirmado:

- Permitir tareas de mantenimiento de un baño sin servicio activo.
- Agregar la relación directa TAREAS_OPERATIVAS -> BANOS_QUIMICOS.
- Permitir servicio opcional para esos mantenimientos.
- Si una tarea tiene servicio, el baño de la tarea debe coincidir con el del servicio.

Ejemplo: reparar la puerta de un baño que está en el depósito genera una tarea para ese baño, sin inventar cliente ni servicio.

Diferencia respecto del ER original: la tarea referencia un servicio y no tiene referencia directa al baño. Ajuste identificado: agregar IdBano y permitir IdServicio nulo para los casos confirmados. Esto no autoriza a dejar sin servicio cualquier tipo de tarea; las reglas para los demás tipos se definirán antes de implementar. No requiere una tabla nueva.

### C3. Condición física y disponibilidad (D5)

Confirmado:

- ESTADOS_BANO representa condición física.
- La disponibilidad se calcula considerando las asignaciones y los mantenimientos que impidan utilizar el baño.
- Una tarea pendiente de limpieza no bloquea por sí sola la disponibilidad.

Ejemplo: un baño sano asignado a una obra no está disponible; uno libre con reparación que impide usarlo tampoco. Tener una limpieza pendiente no equivale automáticamente a estar bloqueado.

Diferencia respecto del ER original y Figma: el catálogo de estado no precisa la separación y las pantallas mezclan condición física con asignación/disponibilidad. Ajuste identificado: precisar el significado de ESTADOS_BANO y obtener disponibilidad desde condición, servicios y mantenimiento, sin guardar un segundo valor que pueda quedar desactualizado.

Actualización: los valores de condición, el indicador de bloqueo y sus efectos se confirmaron posteriormente en C8-C9. No suponer que todo mantenimiento pendiente bloquea ni que una limpieza pendiente lo hace.

### Alcance de los ajustes y próximas decisiones

Las tres decisiones pueden representarse, en principio, manteniendo las 12 tablas: agregar campos/relaciones y precisar reglas en las tablas existentes. No se fuerza ese número si una decisión posterior exige otra estructura; cualquier cambio se consultará antes. No hay implementación ni aprobación automática de los detalles técnicos sugeridos.

Las siguientes tres decisiones se confirmaron posteriormente con las aclaraciones del estudiante que se registran en C4-C6. No son propuestas pendientes.

### C4. Estados de solicitudes y tareas (D6)

Confirmado:

- Solicitudes: Pendiente, Aprobada, Rechazada, Resuelta y Cancelada.
- Tareas: Pendiente, En curso, Realizada y Cancelada.
- Mantener ESTADOS_OPERACION y agregar un campo que distinga el ámbito Solicitud/Tarea. Validar que cada entidad solo use estados de su ámbito; la coincidencia de nombres no hace intercambiables los registros.
- Aprobar no equivale a resolver. Una solicitud que requiera trabajos se resuelve cuando se completan todos los necesarios. Cancelar una tarea no resuelve automáticamente la solicitud ni debe permitir considerar satisfecho un trabajo todavía necesario.
- El administrador puede resolver reclamos o avisos sin tareas registrando una respuesta o explicación.

Ejemplo: aprobar un pedido de tres baños no completa las entregas. Si una entrega se cancela, no alcanza con que las otras dos estén realizadas para dar el pedido por resuelto.

Ajustes respecto del ER: campo de ámbito en ESTADOS_OPERACION y validación de compatibilidad en sus relaciones; utilizar ObservacionesAdministrador para registrar la respuesta/explicación cuando alcance. No se necesita otra tabla para distinguir estos estados. Siguen pendientes la matriz completa de transiciones, quién puede cancelar y cómo registrar sustituciones de trabajos; no se infieren permisos nuevos.

### C5. Modalidades de cambio de ubicación y conservación histórica (D3)

Confirmado:

- Traslado solicitado a la empresa: actualizar ubicación al completar la tarea de traslado.
- Traslado ya realizado informado por el cliente: actualizar ubicación cuando el administrador valide el aviso.
- Conservar ubicación anterior y fecha efectiva del cambio, separada de la fecha de registro o validación.
- Los trabajos realizados anteriormente conservan su referencia a la ubicación que les correspondía.

Ejemplo: el cliente informa el martes un traslado realizado el lunes; el administrador valida el miércoles. La fecha efectiva es el lunes, no el miércoles, y un trabajo realizado antes del traslado mantiene su ubicación anterior.

Ajustes identificados respecto del ER: modalidad explícita en la solicitud; fechas separadas para ocurrencia efectiva y registro/validación; relación de tareas con UBICACIONES_SERVICIO para conservar su contexto histórico. Los nombres y tipos exactos de campos se definirán en la revisión técnica. No sobrescribir la dirección de una fila histórica para representar un traslado ni reasignar retroactivamente trabajos realizados. Las tareas de mantenimiento sin servicio no se obligan por esta decisión a tener una ubicación de servicio. Todo puede representarse inicialmente con las tablas existentes.

### C6. Registro de cliente y cuenta pendiente (D2/D9)

Confirmado:

- Crear juntos usuario y perfil de cliente, con rol Cliente y estado de cuenta Pendiente.
- Hasta la activación administrativa, mostrar solamente el aviso de espera, sin acceso a servicios ni solicitudes.
- La API también impone la restricción; ocultar opciones en la interfaz no es suficiente.
- El usuario no elige su rol ni activa su cuenta.

Ejemplo: aunque una persona recién registrada intente llamar directamente al endpoint de servicios, su cuenta pendiente no puede obtener esos datos.

Ajustes respecto del ER/Figma: las tablas USUARIOS, CLIENTES y ROLES ya permiten representar estos conceptos; precisar el estado inicial, crear los dos registros como una operación indivisible y validar rol/estado desde el servidor. El aviso de espera es una adaptación de pantalla que se implementará en un bloque posterior. No autoriza todavía un mecanismo concreto de autenticación. El identificador de login quedó confirmado en C7. Los datos obligatorios del perfil, política de cambio de rol y alta segura del primer administrador siguen pendientes.

### C7. Correo como único identificador de acceso (D2)

Confirmado por el estudiante: usar correo electrónico único, eliminando espacios exteriores y comparando sin distinguir mayúsculas/minúsculas. No habrá nombre de usuario adicional. Ejemplo: Ana@correo.com y ana@correo.com identifican la misma cuenta.

Ajuste respecto del ER: dejar de utilizar NombreUsuario y omitir ese campo en el modelo a implementar; Email será la credencial única, con normalización y unicidad coherentes en servidor y base. Ajuste de pantalla: cambiar la etiqueta del login a «Correo electrónico». No se modifica el ER original ni las capturas de Figma. Los detalles técnicos de normalización e índices se revisarán antes de implementar.

### C8. Ciclo de vida del servicio y exclusividad de asignación (D10/C3)

Confirmado:

- Una unidad asignada a un servicio no se puede asignar simultáneamente a otro, aunque aún no se haya entregado.
- En esta versión no se implementan reservas futuras superpuestas de una misma unidad.
- Diferenciar servicio Pendiente de entrega, Entregado, Finalizado y Cancelado, conservando las fechas correspondientes.
- Solicitar baja no libera el baño. Al confirmar retiro y cerrar el servicio se libera la asignación; que pueda volver a asignarse depende además de condición y mantenimiento.
- Una cancelación previa a la entrega libera la asignación sin registrar un retiro ficticio.

Ejemplo: un baño preparado para entregar mañana ya tiene una asignación exclusiva. Tras retirarlo se cierra ese servicio, pero si está No apto no puede asignarse otra vez.

Ajuste respecto del ER: Activo y FechaInicio/FechaFin no expresan por sí solos las cuatro situaciones y sus fechas. Incorporar estado explícito y distinguir fechas de asignación, entrega efectiva, retiro/cierre y cancelación; los nombres exactos se revisarán en el esquema final. Propuesta técnica para mantener las 12 tablas: campo de estado del servicio con valores restringidos, sin crear otro catálogo ni reutilizar estados de solicitudes/tareas. Evitar un Activo independiente que pueda contradecir ese estado. Esta representación técnica se presentará en el resumen final; no es una implementación.

### C9. Condición física y mantenimiento que bloquea nuevas asignaciones (C3)

Confirmado:

- ESTADOS_BANO contiene Apto y No apto como condiciones físicas.
- El administrador indica si una tarea de mantenimiento bloquea nuevas asignaciones. Si es bloqueante y está Pendiente o En curso, impide asignar el baño.
- Realizar o cancelar la tarea elimina el bloqueo de esa tarea, sin cambiar automáticamente la condición física a Apto. Otros mantenimientos bloqueantes pueden seguir impidiendo la asignación.
- El administrador confirma los cambios de condición física; el empleado registra el trabajo y sus observaciones.
- Una limpieza pendiente no bloquea por sí sola la disponibilidad.
- Si un baño ya está en servicio pasa a No apto, conserva la asignación y se muestra la situación al administrador. No se cierra ni libera automáticamente el servicio.

Ejemplo: un baño asignado sufre una rotura; se marca No apto y se informa al administrador, pero no desaparece del servicio del cliente. Completar una reparación no basta para marcarlo Apto sin confirmación administrativa.

Ajuste respecto del ER/Figma: separar explícitamente condición y disponibilidad; incorporar el indicador de mantenimiento bloqueante en TAREAS_OPERATIVAS y aplicar permisos administrativos para la condición. La disponibilidad para una nueva asignación requiere condición Apto, ausencia de asignación vigente y ausencia de mantenimiento bloqueante Pendiente/En curso. Se mantiene el catálogo y no se agrega tabla de disponibilidad.

### C10. Datos obligatorios del registro

Confirmado por el estudiante: nombre, apellido, correo electrónico, teléfono y contraseña obligatorios; razón social, CUIT y dirección principal opcionales. El perfil CLIENTES se crea igualmente. El teléfono se guarda como texto para conservar prefijos y signos como +. No imponer CUIT único en esta versión. La dirección necesaria para prestar el servicio se registra en la solicitud correspondiente aunque no exista dirección principal del cliente.

Ajuste respecto del ER: precisar nulabilidad de campos de USUARIOS/CLIENTES; no confundir dirección principal con dirección del servicio. La contraseña recibida es obligatoria, pero se propone guardar solo ContrasenaHash con un mecanismo estándar, nunca texto plano. El algoritmo y la autenticación se resolverán al implementar el bloque de acceso, sin introducir por ello un nombre de usuario adicional.

### C11. Aplicación de tipos de operación (D6)

Confirmado: mantener TIPOS_OPERACION con indicadores de uso en solicitudes, tareas o ambas, y validar el ámbito en la API. Solicitudes: nuevo servicio, baño adicional, limpieza extra, cambio de dirección, reclamo y baja. Tareas: entrega, retiro, limpieza semanal, limpieza extra, traslado y mantenimiento. Limpieza extra puede aplicarse a ambos; no copiar automáticamente un tipo de solicitud a la tarea que produce (baja -> retiro, por ejemplo). La modalidad del cambio de dirección sigue siendo un dato aparte.

Ajuste respecto del ER: agregar los indicadores a TIPOS_OPERACION y definir sus valores, sin crear otro catálogo.

### C12. Tareas pendientes sin empleado y conservación del responsable

Confirmado: permitir tareas Pendientes sin empleado; exigir empleado activo y con rol Empleado antes de pasar a En curso o Realizada. Solo el administrador asigna o reasigna tareas. Los empleados únicamente consultan y registran avances de sus propias tareas. Conservar el empleado asociado a tareas realizadas aunque luego su cuenta sea suspendida o dada de baja.

Ajuste respecto del ER: IdEmpleado opcional mientras la tarea está Pendiente; validación de rol, estado y propiedad en API, y protección de referencias históricas. No borrar el usuario ni poner a nulo su referencia en trabajos realizados. Suspender una cuenta no vuelve inválido el registro histórico; la condición de empleado activo se comprueba al asignar y al avanzar la tarea, no para mostrar hechos ya realizados.

Las políticas restantes de permisos/transiciones, cambio de rol y preparación del primer administrador se precisarán antes de sus bloques funcionales; no se inventan reglas por omisión. Se mantiene la intención de 12 tablas. No crear entidades ni migraciones antes de revisar y cerrar el resumen del modelo.

Verificación de esta actualización: revisar el diff del plan y comparar hashes SHA-256 de los tres originales. Solo cambia este archivo; no corresponde repetir compilación porque no cambió código. El pendiente visual V1 y la falta de prueba app/API continúan vigentes. Esta preparación sigue abierta a las respuestas del estudiante; no comenzar implementación.

Mensaje de commit sugerido para esta actualización documental: `docs: registrar decisiones iniciales del modelo de datos`. No ejecutado.

## 10. Resumen final del esquema para revisión

Estado: preparación documental terminada y propuesta de esquema lista para revisión/cierre por el estudiante. No constituye autorización para crear entidades, bases o migraciones. Las reglas C1-C12 están confirmadas; los nombres, tipos de almacenamiento y restricciones técnicas descritos abajo son propuestas de diseño que expresan esas reglas. Los originales conservan su carácter de documentos aprobados por el profesor; este resumen identifica las adaptaciones acordadas con el estudiante.

Se mantienen exactamente las 12 tablas. PK = clave primaria, FK = referencia a otra tabla. Los campos de la columna principal son obligatorios salvo aclaración. Los campos opcionales pueden volverse obligatorios según operación o estado; opcional en la tabla no significa prescindible en todos los formularios.

### Tablas, campos principales y opcionales

| Tabla | Campos principales propuestos | Opcionales o condicionales |
| --- | --- | --- |
| USUARIOS | IdUsuario PK; Nombre; Apellido; Email; Telefono (texto); ContrasenaHash; IdRol FK; EstadoCuenta; FechaRegistro | MotivoSuspension; FechaBaja. Propuesta técnica: EstadoCuenta limitado a Pendiente, Activa, Suspendida y BajaLogica; estos nombres de almacenamiento no crean roles nuevos. |
| ROLES | IdRol PK; NombreRol | Sin opcionales principales. Catálogo Administrador, Empleado y Cliente. |
| CLIENTES | IdCliente PK; IdUsuario FK, único | RazonSocial; Cuit (texto, no único); DireccionPrincipal; Observaciones. |
| BANOS_QUIMICOS | IdBano PK; CodigoInterno único; IdTipoBano FK; IdEstadoBano FK; FechaAlta | Observaciones. No guardar Disponible como indicador independiente. |
| TIPOS_BANO | IdTipoBano PK; NombreTipo | Descripcion. Clasificación obra/evento; no fija el uso permitido del baño. |
| ESTADOS_BANO | IdEstadoBano PK; NombreEstado | Descripcion. Solo condición física: Apto y No apto. |
| SERVICIOS | IdServicio PK; IdCliente FK; IdBano FK; TipoUso (obra/evento); CantidadLimpiezasSemana; EstadoServicio; FechaAsignacion | IdSolicitudOrigen FK; FechaEntregaProgramada; FechaEntregaReal; FechaRetiroReal; FechaFinalizacion; FechaCancelacion; Observaciones. Fechas reales obligatorias al alcanzar el estado correspondiente. |
| UBICACIONES_SERVICIO | IdUbicacion PK; IdServicio FK; Direccion; FechaRegistro; EsActual | Localidad; Referencia; FechaDesde (efectiva); FechaHasta (efectiva); FechaValidacion. FechaDesde obligatoria al entrar en vigencia; puede faltar en un destino aún pendiente de entrega. |
| SOLICITUDES_CLIENTE | IdSolicitud PK; IdCliente FK; IdTipoOperacion FK; IdEstadoOperacion FK; FechaSolicitud | IdServicioReferencia FK (precisa el antiguo IdServicio); Descripcion; CantidadBanos; IdTipoBanoSolicitado FK; TipoUsoSolicitado; DireccionSolicitada; FechaDeseada; NuevaDireccion; ModalidadCambioDireccion; FechaCambioEfectiva; FechaValidacion; FechaResolucion; ObservacionesAdministrador. Obligatoriedad según reglas siguientes. |
| TAREAS_OPERATIVAS | IdTarea PK; IdBano FK; IdTipoOperacion FK; IdEstadoOperacion FK; FechaRegistro; BloqueaAsignacion (booleano) | IdServicio FK; IdEmpleado FK a USUARIOS; IdSolicitud FK; IdUbicacion FK; IdUbicacionDestino FK; FechaProgramada; FechaInicioReal; FechaRealizada; FechaCancelacion; PapelHigienicoRepuesto (booleano opcional); ObservacionesEmpleado. Reglas condicionales abajo. |
| TIPOS_OPERACION | IdTipoOperacion PK; NombreOperacion; AplicaASolicitudes; AplicaATareas | Descripcion. Al menos un indicador verdadero. Valores por C11. |
| ESTADOS_OPERACION | IdEstadoOperacion PK; NombreEstado; Ambito (Solicitud/Tarea) | Descripcion. Valores por C4; unicidad propuesta por Ambito + NombreEstado. |

Propuestas técnicas comunes: identificadores enteros; textos Unicode con longitudes acotadas a definir en el mapeo; teléfono y CUIT nunca numéricos; fechas de hechos/registro con hora y criterio temporal uniforme. Mantener separadas fechas efectivas informadas y fechas de registro/validación. Los tamaños exactos y nombres C# no necesitan otra decisión de negocio.

No se afirma que cada campo adicional haya sido confirmado por su nombre: son una representación técnica para revisar. Por ejemplo, FechaInicioReal, fechas de resolución/cancelación y las dos referencias de ubicación permiten expresar cronología sin nuevas tablas.

### Relaciones y cardinalidades

- ROLES 1 -> muchos USUARIOS. Un usuario tiene un rol; puede tener cero o un perfil CLIENTES, y cada perfil pertenece a un único usuario. Todo autorregistro crea ambos registros como una operación indivisible, con rol Cliente y cuenta Pendiente.
- CLIENTES 1 -> muchos SERVICIOS y SOLICITUDES_CLIENTE. BANOS_QUIMICOS 1 -> muchos SERVICIOS históricos, pero como máximo una asignación vigente por baño.
- TIPOS_BANO y ESTADOS_BANO 1 -> muchos BANOS_QUIMICOS. TIPOS_BANO también clasifica el tipo pedido en una solicitud, sin exigir que el tipo físico coincida con el uso obra/evento.
- SERVICIOS 1 -> muchas UBICACIONES_SERVICIO. Solo una ubicación actual por servicio cuando corresponda; no se reemplazan direcciones históricas al trasladar.
- SOLICITUDES_CLIENTE 1 -> muchos SERVICIOS originados y muchas TAREAS_OPERATIVAS. Un servicio puede no tener solicitud de origen (por ejemplo, alta administrativa); no se inventa una solicitud para completar la FK. Una solicitud puede tener cero o un servicio de referencia. Este vínculo no se confunde con los nuevos servicios que origina.
- BANOS_QUIMICOS 1 -> muchas TAREAS_OPERATIVAS. El servicio de la tarea es opcional solo para los mantenimientos sin servicio acordados; si existe, su baño debe coincidir con IdBano de la tarea.
- USUARIOS 1 -> muchas tareas asignadas como empleado; una tarea puede estar sin asignar mientras está Pendiente. No se agrega tabla EMPLEADOS ni empleado fijo en SERVICIOS.
- Cada solicitud/tarea tiene un tipo y un estado compatibles con su ámbito. Las ubicaciones de tarea se vinculan a UBICACIONES_SERVICIO; cuando hay servicio deben pertenecer al mismo servicio de la tarea.

### Restricciones y criterios de comprobación del futuro modelo

1. Correo: quitar espacios exteriores, comparar sin distinguir mayúsculas y hacer cumplir unicidad también en base de datos. No utilizar NombreUsuario. No guardar contraseñas en claro. No agregar credenciales reales a archivos versionados.
2. Registro: Nombre, Apellido, Email, Telefono y contraseña recibida obligatorios/no vacíos. El perfil cliente existe aunque sus datos comerciales sean nulos. CUIT sin índice único. Cuenta Pendiente solo accede al aviso de espera, no a operaciones de servicios/solicitudes; la API impone la misma regla y no acepta rol ni activación elegidos por el cliente.
3. Pedido nuevo/adicional: cantidad entera positiva, un tipo solicitado, uso, dirección y fecha deseada. La dirección de prestación es obligatoria en la solicitud aunque el perfil no tenga dirección principal. Para adicional, servicio de referencia obligatorio y del mismo cliente. Cada baño asignado origina un servicio distinto vinculado a esa solicitud. No asignar unidades antes de su selección/aprobación administrativa. No equiparar cantidad solicitada con trabajos ya completados.
4. Operaciones sobre un servicio: servicio de referencia del mismo cliente. En cambio de dirección exigir modalidad y nueva dirección; en aviso de traslado realizado exigir fecha efectiva informada. Fechas de validación/resolución se completan cuando suceden. Reclamos/avisos resueltos sin tareas requieren respuesta administrativa. Una solicitud sin tareas no se resuelve automáticamente solo por tener cero tareas; el administrador debe resolverla según C4.
5. Estados/tipos: API valida ámbito de estados y los indicadores del tipo, además de las FKs. Una FK válida no basta para garantizar compatibilidad. Propuesta de restricciones de base/validación de servidor para reforzar integridad, sin una tabla adicional por catálogo.
6. Servicio: campo EstadoServicio restringido a Pendiente de entrega, Entregado, Finalizado y Cancelado. Sustituye Activo como dato independiente; una asignación vigente se deriva de los dos primeros estados. Propuesta de índice único filtrado por esos estados para IdBano, junto con operaciones transaccionales y validación de disponibilidad. No permitir dos asignaciones simultáneas ni reservar la misma unidad para otro servicio mientras ya tiene asignación. Conservar fechas programadas separadas de las reales; cancelar antes de entrega no registra retiro. La solicitud de baja por sí sola no cambia el estado ni libera la unidad.
7. Disponibilidad calculada: Apto, sin asignación vigente y sin mantenimiento bloqueante Pendiente/En curso. BloqueaAsignacion solo puede ser verdadero en mantenimiento y lo define el administrador. Realizar/cancelar ese mantenimiento no marca el baño Apto. Cambiar condición requiere administrador. Un baño asignado que pasa a No apto conserva servicio y referencia al cliente; se muestra la situación al administrador sin liberar automáticamente.
8. Ubicaciones: cerrar el intervalo anterior y agregar la nueva fila al hacer efectivo el traslado; no sobrescribir su dirección. FechaHasta no anterior a FechaDesde; evitar intervalos efectivos superpuestos y más de una fila actual. FechaRegistro/FechaValidacion no sustituyen la fecha efectiva. Un aviso retroactivo se valida preservando trabajos históricos; no reasignarlos silenciosamente por recalcular la ubicación actual.
9. Contexto del trabajo: propuesta de IdUbicacion para el lugar al que corresponde la tarea; en traslado, IdUbicacionDestino conserva además el destino. En entrega puede referirse al destino registrado antes de entrar en vigencia. Una tarea realizada conserva esas referencias; trabajos antiguos no se redirigen a la ubicación actual. Mantenimiento sin servicio puede carecer de ambas referencias y describir el lugar en observaciones; no crear un servicio ficticio. Estas referencias precisan C5 sin crear tabla de movimientos.
10. Responsable: Pendiente permite IdEmpleado nulo; En curso/Realizada requieren empleado activo con rol Empleado al avanzar. Solo administrador asigna/reasigna. El empleado solo ve/modifica sus propias tareas conforme a los permisos acordados. No reasignar retrospectivamente una tarea realizada; conservar su empleado aunque la cuenta deje de estar activa. Propuesta técnica de FKs sin borrado en cascada para historial y baja lógica de cuentas.
11. Resolución: aprobar no resuelve; tareas canceladas no satisfacen trabajos necesarios. La futura validación debe comprobar el resultado requerido (por ejemplo, las entregas de todos los baños del pedido), no limitarse a que todas las tareas no canceladas estén realizadas. Sustituir una tarea cancelada conserva su registro. No se agrega una tabla de pasos para esta versión; la regla de resolución por tipo y la intervención administrativa se detallarán en el bloque funcional antes de automatizarla.
12. Integridad histórica: conservar cuentas, servicios, ubicaciones, solicitudes y tareas referenciadas; evitar borrados físicos en cascada. En vínculos solicitud/servicio, validar mismo cliente; en tarea/servicio, mismo baño; en tarea/ubicación, mismo servicio. Las actualizaciones de entrega/retiro/traslado y sus fechas deben ser coherentes dentro de una transacción. Estos son criterios de futuras pruebas, no pruebas ya ejecutadas.

### Diferencias respecto del ER original aprobado

| Área | Adaptación a revisar con el profesor |
| --- | --- |
| Acceso | Omitir NombreUsuario, usar Email único normalizado y cambiar etiqueta de login. Proponer ContrasenaHash en lugar de Contrasena. Precisar campos obligatorios y perfil cliente con datos comerciales opcionales. |
| Estados y tipos | Agregar Ambito a ESTADOS_OPERACION e indicadores de uso a TIPOS_OPERACION, manteniendo ambos catálogos compartidos. |
| Solicitudes | Agregar cantidad, tipo solicitado, uso, dirección de prestación y fecha deseada; distinguir servicio de referencia de servicios originados. Agregar modalidad de traslado y separar fechas efectivas de registro/validación/resolución. |
| Servicios | Agregar solicitud de origen, cuatro estados y fechas del ciclo de vida. Sustituir Activo independiente y precisar FechaInicio/FechaFin mediante fechas con significado explícito. Exclusividad de asignación por baño. |
| Tareas | Agregar baño directo; servicio opcional para mantenimiento sin servicio; empleado opcional en Pendiente; indicador de bloqueo; referencias de ubicación y fechas de seguimiento. Preservar responsable histórico. |
| Ubicaciones | Precisar intervalos efectivos, fecha de registro/validación y referencia estable desde trabajos anteriores; destino previo a entrega no implica ubicación efectiva. |
| Condición/disponibilidad | ESTADOS_BANO solo Apto/No apto. Disponibilidad calculada; no crear tabla ni columna independiente para ella. |

Sin cambios de alcance: sin pagos/facturación; sin tabla de empleados, contratos comerciales, empresa compartida entre varias cuentas, auditoría general o motor de reservas. La falta de CUIT único no crea por sí misma relaciones entre distintos perfiles cliente.

### Programación manual de tareas: propuesta pendiente de confirmación

Propuesta para esta primera versión: el administrador crea/programa y asigna las tareas manualmente, incluidas las limpiezas semanales. La frecuencia del servicio sirve de referencia para programarlas; no genera automáticamente tareas ni calcula recorridos. Aprobar una solicitud no crea por sí solo trabajos si el administrador no los registra. Esta última descripción del flujo manual sigue siendo propuesta, no regla confirmada.

Sí requiere confirmación del estudiante antes de implementar la programación de tareas. No impide revisar/cerrar el esquema: FechaProgramada y las relaciones existentes admiten el flujo manual sin tablas adicionales. No se implementa planificación automática por omisión.

### Resultado y verificaciones de esta preparación

No queda una regla de negocio indispensable que obligue a abrir otra ronda para expresar este esquema de 12 tablas. Las propuestas técnicas se presentan juntas para revisión, no como decisiones ya confirmadas. Programación manual, matriz detallada de permisos/transiciones, alta segura del primer administrador y método de autenticación se concretarán antes de sus bloques funcionales; la revisión del esquema no autoriza su implementación ni agrega requisitos.

Cambió únicamente docs/plan-trabajo.md. Verificado: revisión de consistencia C1-C12, inventario de 12 tablas, diff de Git y hashes de los originales. No se ejecutó compilación ni pruebas de base porque no cambió código ni existe modelo implementado. No se crearon entidades, DbContext, base, migraciones, commits ni publicaciones. LocalDB fue detectado anteriormente con consultas de solo lectura: instancia MSSQLLocalDB 15.0.4382.1 detenida; no se inició ni se probó conexión SQL.

Entrega documental cerrada; esquema pendiente de revisión y cierre por el estudiante. Esperar su confirmación antes del siguiente bloque. Mensaje de commit sugerido: `docs: consolidar esquema de datos y decisiones confirmadas`.
