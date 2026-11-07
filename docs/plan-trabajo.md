# Plan de trabajo

Fecha de revisión: 28/10/2026. Preparación, estructura, persistencia y autenticación de API/app cerradas. Gestión de cuentas implementada para revisión: ver sección 14. Las secciones anteriores conservan la historia; no autorizan otros bloques funcionales.

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

### Programación manual de tareas: confirmada posteriormente

Confirmado para esta primera versión: el administrador crea/programa y asigna las tareas manualmente, incluidas las limpiezas semanales. La frecuencia del servicio sirve de referencia para programarlas; no genera automáticamente tareas ni calcula recorridos. No se implementa generación automática de trabajos al aprobar solicitudes.

El estudiante confirmó la programación manual al autorizar el bloque de modelo/persistencia. Ya no representa una decisión pendiente. Esta confirmación no autoriza todavía endpoints de tareas ni pantallas.

### Resultado y verificaciones de esta preparación

No queda una regla de negocio indispensable que obligue a abrir otra ronda para expresar este esquema de 12 tablas. Las propuestas técnicas se presentan juntas para revisión, no como decisiones ya confirmadas. Programación manual, matriz detallada de permisos/transiciones, alta segura del primer administrador y método de autenticación se concretarán antes de sus bloques funcionales; la revisión del esquema no autoriza su implementación ni agrega requisitos.

Cambió únicamente docs/plan-trabajo.md. Verificado: revisión de consistencia C1-C12, inventario de 12 tablas, diff de Git y hashes de los originales. No se ejecutó compilación ni pruebas de base porque no cambió código ni existe modelo implementado. No se crearon entidades, DbContext, base, migraciones, commits ni publicaciones. LocalDB fue detectado anteriormente con consultas de solo lectura: instancia MSSQLLocalDB 15.0.4382.1 detenida; no se inició ni se probó conexión SQL.

Entrega documental cerrada; esquema pendiente de revisión y cierre por el estudiante. Esperar su confirmación antes del siguiente bloque. Mensaje de commit sugerido: `docs: consolidar esquema de datos y decisiones confirmadas`.

## 11. Bloque de modelo de datos y persistencia: implementado y cerrado

El estudiante confirmó el esquema de la sección 10 y la programación manual (decisión C13), e informó que guardó y subió el avance documental. Autorizó las entidades, contexto, configuraciones, migración inicial y creación exclusiva de BanosQuimicosDb. No autorizó autenticación, CRUD ni pantallas nuevas. Esta autorización posterior reemplaza las restricciones de la etapa documental para este bloque únicamente.

### Rama, archivos y decisiones técnicas

- Se revisaron AGENTS.md, el plan completo y Git. El árbol estaba limpio, en master, commit 3f0fdfe. Se creó feature/modelo-datos desde esa versión, sin descartar cambios. No se hizo commit ni push.
- Modelos/: 12 clases de entidades con propiedades y referencias de navegación. No se agregaron entidades fuera del esquema.
- Datos/BanosQuimicosDbContext.cs y Datos/Configuraciones/: contexto y 12 configuraciones separadas, con claves, longitudes, nulabilidad, tablas, relaciones y catálogos. Todas las relaciones usan NO ACTION en SQL Server, evitando borrados en cascada.
- Datos/Migraciones/20261027203511_Inicial.cs, su Designer y snapshot: migración inicial revisada, con 12 tablas del negocio, índices y 27 filas de catálogos. No incluye usuarios ni datos operativos.
- BanosQuimicos.Api.csproj: EF Core SqlServer y Design 10.0.12, con Design privado; dotnet-tools.json: herramienta dotnet-ef 10.0.12 local al repositorio, sin instalación global. Compatibilidad net10.0 verificada en NuGet y mediante compilación.
- Una dependencia transitiva inicial de SqlClient generó avisos NU1902/NU1903 por System.Security.Cryptography.Pkcs 9.0.11. Se fijó la versión corregida 10.0.13; la auditoría posterior de la API no encontró paquetes vulnerables en los orígenes consultados.
- Program.cs registra UseSqlServer. La conexión de desarrollo usa (localdb)\MSSQLLocalDB, BanosQuimicosDb y seguridad integrada de Windows; no se guardan contraseñas. No se ejecutan migraciones automáticamente al arrancar.
- scripts/verificar-persistencia.sql comprueba metadatos, catálogos y tablas operativas vacías, sin insertar usuarios ni datos de prueba. docs/base-datos.md explica estructura, reproducción, límites y consulta por Visual Studio/SSMS; README actualizado.

Tipos/longitudes elegidos: claves int; textos Unicode nvarchar; nombre/apellido 100, email 254, teléfono 40, CUIT 20, razón social 200, dirección 300, observaciones 2000. Hash hasta 1000 caracteres. Fechas DateTimeOffset/datetimeoffset para distinguir instante efectivo de registro/validación. Teléfono y CUIT siguen siendo texto. No se implementó un algoritmo de hash ni autenticación todavía: solo su campo de persistencia.

### Protecciones implementadas en SQL Server

- Email único con collation Latin1_General_100_CI_AS y CHECK que rechaza vacío/espacios exteriores. La propiedad C# recorta espacios; no hay NombreUsuario.
- CódigoInterno único, sin espacios exteriores; CLIENTES.IdUsuario único; CUIT no único.
- Índice único filtrado UX_SERVICIOS_BanoAsignado sobre IdBano para Pendiente de entrega y Entregado. Permite conservar servicios históricos sin duplicar asignaciones vigentes.
- Índice único filtrado para una ubicación actual por servicio; chequeo básico de intervalo efectivo.
- Restricciones de uso obra/evento, modalidad solicitado/informado, cuenta y fechas/ciclo del servicio; frecuencia no negativa y cantidad solicitada positiva si se proporciona.
- Las tareas sin servicio y los bloqueos se limitan al tipo Mantenimiento; En curso/Realizada exigen IdEmpleado no nulo. Los CHECK de tareas referencian IDs estables de catálogos (11 mantenimiento, 7/8 estados); cambios a esos IDs requieren migraciones coordinadas.
- 21 relaciones FK activas, validadas y NO ACTION. Estas protecciones no sustituyen baja lógica ni impiden por sí mismas modificar un historial; los futuros servicios de negocio deben conservarlo.

### Base y migración aplicada

Se inició la instancia existente MSSQLLocalDB, versión 15.0.4382.1 (SQL Server 2019). La consulta previa comprobó que BanosQuimicosDb no existía. No se modificaron bases de otros proyectos ni se instalaron componentes de SQL Server.

La primera actualización con --no-build usó el ensamblado anterior a generar la migración y creó solo la base vacía con __EFMigrationsHistory. Se detectó el resultado, se compiló la migración y se consultó esa base antes de continuar: solo tenía el historial vacío. Después se aplicó Inicial correctamente, sin borrar ni recrear la base. Es un incidente de ejecución corregido, no una migración pendiente.

Resultado: 12 tablas del negocio más __EFMigrationsHistory, tabla técnica de EF que guarda MigrationId/ProductVersion. Su fila es 20261027203511_Inicial / 10.0.12. Las 13 tablas visibles no significan una entidad adicional del negocio.

Catálogos comprobados: ROLES 3, TIPOS_BANO 2, ESTADOS_BANO 2, TIPOS_OPERACION 11, ESTADOS_OPERACION 9. Total 27 filas, con nombres, ámbitos e indicadores acordados. USUARIOS, CLIENTES, BANOS_QUIMICOS, SERVICIOS, UBICACIONES_SERVICIO, SOLICITUDES_CLIENTE y TAREAS_OPERATIVAS permanecen vacías.

### Verificaciones realizadas y pendientes

Realizadas:

- Compilación de BanosQuimicos.sln (API, app Android y Windows): correcta, 0 errores y 0 advertencias tras corregir la dependencia.
- Conexión real con LocalDB mediante sqlcmd y EF Core; migración generada/revisada/aplicada.
- Verificación SQL de solo lectura: 12 tablas de negocio + historial, 21 FKs sin cascada, índices únicos/filtros, collation de email, CHECK activos y catálogos/ausencia de datos operativos.
- dotnet ef migrations has-pending-model-changes: sin diferencias entre el modelo y la última migración.
- Auditoría NuGet de la API con dependencias transitivas: sin vulnerabilidades informadas en los orígenes actuales.
- API ejecutada con perfil http; GET /api/estado respondió HTTP 200 y el mensaje esperado. El endpoint sigue siendo una respuesta simple, no una comprobación de salud de la base. Se detuvo el servidor de prueba.
- Revisión de Git y hashes SHA-256: PDF y dos PNG originales conservados. bin/obj/.vs continúan excluidos.

Pendiente para próximos bloques: autenticación/activación y permisos; ámbito real del tipo/estado de cada solicitud/tarea; datos condicionales de solicitudes; comprobación del mismo cliente/baño/servicio en relaciones; disponibilidad completa y concurrencia al asignar; transiciones y resolución de solicitudes; programación manual; actualizaciones históricas de ubicación; impedir cambios retroactivos de responsable. Las FKs garantizan existencia, no esas reglas entre registros. Detalle y pasos reproducibles en docs/base-datos.md.

No se realizaron altas de usuarios/datos operativos ni pruebas CRUD; el control de índices se hizo consultando su definición real, no insertando duplicados. No se ejecutó la interfaz en este bloque; se conservan las pruebas previas del estudiante en Windows/Android y el pendiente visual V1. App/API sigue sin integración ni prueba de conexión desde el teléfono.

Bloque cerrado dentro del alcance autorizado. Para revisar: abrir solución, consultar la base desde Visual Studio/SSMS y ejecutar scripts/verificar-persistencia.sql siguiendo docs/base-datos.md. No se hizo commit ni push. Mensaje sugerido: `feat: agregar modelo de datos y persistencia con EF Core y LocalDB`. Esperar confirmación del estudiante antes de comenzar otro bloque.

## 12. Bloque de autenticación de la API: cerrado para revisión

El estudiante informó commit, push y merge de persistencia y actualización de master, y autorizó registro, login, roles, restricción de cuentas no activas, consulta de identidad, activación administrativa y preparación del primer administrador. No autorizó pantallas, CRUD del negocio, recuperación de contraseña ni proveedores externos.

### Estado inicial y rama

Se releyeron AGENTS.md y el plan, y se revisó Git: master limpio en 12bf0ba (merge de persistencia), coincidente con la referencia local origin/master. Se creó feature/autenticacion-api desde ese master. No se descartaron cambios, no se hizo commit ni push.

### Elecciones técnicas explicadas antes de implementar

- JWT Bearer con paquete oficial Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12. Se valida firma HMAC-SHA256, emisor, audiencia y expiración; duración predeterminada 30 minutos y tolerancia de reloj 15 segundos. No hay refresh tokens ni tablas de sesiones.
- PasswordHasher<Usuario> de ASP.NET Core para hash con sal y verificación estándar; se usa este servicio de Identity sin agregar las tablas de Identity. Se actualiza un hash al iniciar sesión si el verificador indica SuccessRehashNeeded.
- Política técnica de contraseña: 12 a 128 caracteres, sin recortar ni exigir símbolos concretos. Nombre/apellido/correo/teléfono se recortan y validan; opcionales comerciales mantienen las longitudes del modelo. No se acepta rol ni activación del solicitante.
- Clave JWT aleatoria de 64 bytes guardada en User Secrets por scripts/configurar-jwt-local.ps1, sin imprimirla ni pasarla como argumento. El script conserva una clave existente. UserSecretsId del csproj es un identificador público, no la clave. En otros entornos la clave se proporciona externamente como Jwt__ClaveBase64. El arranque normal valida la configuración y no tiene una clave fija de respaldo.
- No se modificaron entidades, configuraciones EF, snapshot ni migración: se conservan 12 tablas del negocio más el historial técnico de EF. No fue necesario un cambio de esquema.

### Implementación

- Contratos/Autenticacion/: DTOs separados de EF, validación y respuestas limitadas sin hashes. El registro rechaza campos JSON no admitidos, incluidos rol/estado.
- POST /api/autenticacion/registro: crea USUARIOS y CLIENTES juntos en transacción explícita; rol Cliente consultado del catálogo, estado Pendiente y fecha UTC. Normaliza espacios exteriores del correo y respeta la comparación sin mayúsculas de SQL Server. Precomprueba duplicados y captura también la colisión del índice único concurrente como 409. No se expone la contraseña recibida.
- POST /api/autenticacion/login: verifica correo/contraseña con mensaje genérico para credenciales incorrectas (401). Pendiente obtiene token restringido y aviso; Activa obtiene token; Suspendida/BajaLogica con contraseña correcta recibe 403 sin token.
- GET /api/autenticacion/yo: cuenta activa obtiene DTO propio con datos básicos/rol/estado; pendiente recibe solo estado y aviso de espera, no perfil ni datos operativos. No devuelve entidades ni hash.
- POST /api/cuentas/{id}/activar: exige administrador activo y actualiza solo Cliente/Pendiente con perfil existente, mediante UPDATE condicional. 204 al activar, 404 si no existe y 409 si no cumple condiciones. No reactiva suspendidas ni cambia roles.
- Seguridad/ConfiguracionSeguridad.cs: por cada token válido consulta usuario/rol actuales en SQL Server y reconstruye los claims. Inexistente/Suspendida/BajaLogica falla autenticación (401). Pendiente no obtiene rol operativo. Un token previo deja de dar acceso al suspender/dar de baja y refleja cambios de rol en la petición siguiente.
- CuentaActiva es política predeterminada y fallback; CuentaIdentificada se reserva para /yo; Administrador exige estado activo y rol actual. AllowAnonymous se limita a estado, registro y login. No basta con ocultar opciones de la interfaz. La comprobación es por petición, no una revocación permanente independiente: si una cuenta vuelve legítimamente a un estado permitido, se evalúa ese estado actual y el vencimiento del token.
- El comando local --crear-admin-local pide datos y contraseña sin mostrarla y con confirmación, solo en Development; crea el primer Administrador/Activa en transacción Serializable y termina sin iniciar web. Rechaza si ya existe cualquier administrador o el correo pertenece a otra cuenta. No existe endpoint de bootstrap, credencial fija ni creación automática en el arranque. No crea perfil cliente ficticio para el administrador.
- Program.cs agrega autenticación antes de autorización. Fuera de Development activa redirección HTTPS; HTTP se mantiene únicamente para pruebas locales. /api/estado continúa público y sin dependencia de autenticación.

### Pruebas realizadas y conservación de datos

Se agregó BanosQuimicos.Api.Pruebas a la solución (tercer proyecto, solo de pruebas), con xUnit v3, WebApplicationFactory/TestServer y SQL Server real. Las rutas auxiliares de prueba se agregan exclusivamente al host de pruebas; no existen en la API distribuida. Cada ejecución crea una base nueva BanosQuimicosAuthPruebas_<GUID>, verifica que no exista y aplica la migración original. Nunca usa la base principal, borra registros ni ejecuta EnsureDeleted. Las cuentas llevan PRUEBA AUTENTICACION y correos example.invalid; las contraseñas y la clave JWT se generan en memoria.

Incidente inicial de entorno: SqlLocalDB start MSSQLLocalDB falló y el primer intento de las 11 pruebas falló en el constructor de la infraestructura, antes de probar rutas. Se inspeccionó el log y se comprobó que el proceso existente sí respondía por su canal local. Se verificó @@SERVERNAME, sin recrear ni reparar la instancia. Las ejecuciones iniciales posteriores usaron ese canal mediante BANOS_PRUEBAS_SERVIDOR_SQL; la conexión versionada continuó (localdb)\MSSQLLocalDB. Ese acceso temporal fue retirado tras resolver la desincronización como se documenta en el cierre complementario de esta sección.

Resultado final: 12 pruebas de integración correctas:

1. Registro normaliza correo, crea perfil, guarda/verifica hash estándar y no lo expone.
2. Correo duplicado, incluso concurrente, devuelve 409 sin perfiles duplicados/huérfanos.
3. Falta de teléfono y envío de rol/estado privilegiados devuelven 400 sin alta.
4. Contraseña incorrecta y correo inexistente devuelven el mismo 401.
5. Pendiente solo obtiene estado/aviso; falla acceso operativo, fallback y autoactivación (403).
6. Administrador activa; cliente no activa; repetición incompatible da 409 y objetivo inexistente 404.
7. Cuenta Suspendida con token anterior pierde acceso y no obtiene nuevo token.
8. Cuenta BajaLogica con token anterior pierde acceso y no obtiene nuevo token.
9. Cambio de rol se refleja con el mismo token; no conserva permisos anteriores.
10. Sin token y con token alterado, vencido, emisor/audiencia incorrectos se rechaza acceso; /api/estado continúa público.
11. Bootstrap crea el primer administrador en la base de prueba y rechaza otro sin modificar cuentas.
12. Fallo controlado después de SaveChanges y antes del commit revierte usuario y perfil juntos.

Se conservan las dos bases de las ejecuciones correctas: BanosQuimicosAuthPruebas_00538f787a894d588214e98f337b5990 y BanosQuimicosAuthPruebas_bc8d47134aad4267bc9b6fa2aa4dd24b. No se guardaron sus contraseñas en el repositorio ni se eliminaron datos. La base principal BanosQuimicosDb tenía 0 usuarios y 0 clientes antes y mantiene 0/0 después. No se creó allí un administrador real: el desarrollador elegirá sus credenciales en el comando interactivo.

Verificado además: solución completa (API, pruebas, app Android/Windows) compilada con 0 errores y 0 advertencias; auditoría del proyecto de pruebas con dependencias transitivas sin vulnerabilidades informadas; EF sin cambios pendientes del modelo. Script de clave local ejecutado correctamente, con valor fuera de Git. API ejecutada con User Secrets y conexión temporal al canal local de la instancia existente: /api/estado devolvió 200, /api/autenticacion/yo y activación sin token devolvieron 401. Estos últimos controles HTTP no insertaron datos en la base principal.

### Reproducción, límites y cierre

docs/autenticacion-api.md contiene configuración, bootstrap, endpoints, códigos HTTP, pruebas manuales sin contraseñas fijas y comando dotnet test. README y docs/base-datos.md enlazan el nuevo bloque. Los documentos originales se conservan, con hashes SHA-256 sin cambios. No se guardan tokens, contraseñas ni claves en código/configuración versionada.

En el cierre inicial quedaba pendiente el ingreso interactivo del primer administrador y la prueba manual: el estudiante posteriormente confirmó ambos, según el cierre final de esta sección. El ingreso por consola no fue automatizado con credenciales reales. El arranque por nombre de LocalDB quedó resuelto en el cierre complementario siguiente. Continúan pendientes integración MAUI/API, pantallas de autenticación y pendiente visual V1. No se implementaron endpoints de suspensión/cambio de rol ni CRUD del negocio; los escenarios de suspensión/rol se probaron modificando exclusivamente los datos propios de bases aisladas.

Bloque cerrado para revisión. No se hizo commit ni push. Mensaje sugerido: `feat: agregar autenticacion y activacion de cuentas en la API`. Esperar confirmación antes de continuar con otro bloque.

### Cierre complementario: arranque normal de LocalDB resuelto

El estudiante pidió resolver este pendiente antes del cierre definitivo, sin otro bloque ni acciones destructivas. Se conservaron todos los cambios locales de feature/autenticacion-api.

Diagnóstico: SqlLocalDB info indicaba Stopped y el registro del usuario anunciaba LOCALDB#7AB46D6F, mientras el proceso 19936 seguía ejecutándose como LOCALDB#F0CB2303 sobre los mismos archivos/directorio de MSSQLLocalDB. Ese motor respondía por su canal. La desincronización impedía resolver/iniciar por el nombre normal. Se comprobó esta inconsistencia; no se determinó el evento original que la produjo ni se atribuyó a corrupción o a coexistencia de versiones sin evidencia.

Acción no destructiva: verificar servidor/archivos, bases y sesiones; sin transacciones de usuario abiertas, ejecutar SHUTDOWN normal (sin NOWAIT) en el proceso identificado, esperar que termine e iniciar SqlLocalDB start MSSQLLocalDB. No se borraron ni recrearon instancia/bases; no se alteró registro, versión o archivos MDF/LDF ni se mataron procesos. Una conexión ociosa de Visual Studio fue desconectada por la parada normal.

Verificaciones posteriores:

- SqlLocalDB info informa Running, versión 15.0.4382.1. Consultas desde procesos independientes por (localdb)\MSSQLLocalDB abren BanosQuimicosDb, leen roles y usuarios sin errores.
- La API arrancó con appsettings.Development.json normal y User Secrets solo para la clave JWT, sin ConnectionStrings__BanosQuimicos ni canal alternativo. GET /api/estado devolvió 200. Un login de diagnóstico con correo aleatorio inexistente devolvió 401 con el mensaje esperado y dejó evidencia del SELECT real USUARIOS/ROLES de EF Core en el log. No creó ni cambió cuentas.
- Se retiró la alternativa BANOS_PRUEBAS_SERVIDOR_SQL de la infraestructura de pruebas. Las 12 pruebas de integración pasaron usando únicamente el nombre normal; la ejecución generó y conservó BanosQuimicosAuthPruebas_f319f196af514cbe87260a3a7473d0e4.
- Las tres bases previas conservaron database_id y create_date. BanosQuimicosDb mantiene id 8, 13 tablas (12 del negocio + historial EF), la misma migración y 0 usuarios/0 clientes. No se perdió información ni se creó el administrador real.

Documentación: docs/localdb.md describe evidencia, recuperación realizada, comprobación normal y precauciones ante una recurrencia. No presenta el canal como solución de conexión permanente. docs/guia-postman-autenticacion.md guía los siete pasos pedidos con rutas/cuerpos reales, secretos locales y códigos HTTP: creación personal del administrador, ejecución en VS, login, registro Pendiente, activación, /yo activo y rechazo de activación de otra cuenta por un cliente. Aclara que no existen todavía endpoints de servicios/solicitudes y que la restricción operativa genérica se verifica en el host de integración, sin inventar rutas de negocio.

El arranque por nombre ya no es pendiente. La guía quedó inicialmente a disposición del estudiante; sus pruebas manuales fueron confirmadas después y se registran a continuación. No se presentan como ejecuciones del agente. Se detuvo la API de comprobación para dejar libre el puerto al estudiante. No se inició otro bloque ni se hizo commit/push. Se mantiene el mensaje de commit sugerido del bloque de autenticación.

### Cierre final: pruebas manuales confirmadas por el estudiante

El estudiante informó haber realizado personalmente las siguientes verificaciones en Visual Studio y Postman. Son resultados manuales comunicados por él, separados de las 12 pruebas automatizadas de integración ejecutadas anteriormente por el agente:

| Verificación manual | Resultado confirmado |
| --- | --- |
| Crear el administrador local y arrancar normalmente la API | Completado; uso de la conexión normal de desarrollo. |
| Login del administrador y GET /api/autenticacion/yo | 200; rol Administrador y cuenta Activa. |
| Registro de cliente | 201; cuenta Pendiente. |
| Login del cliente pendiente y consulta /yo | Únicamente estado y aviso de espera, sin datos operativos ni perfil. |
| Intento de autoactivación por cliente | 403. |
| Activación del cliente por administrador | 204. |
| Consulta /yo con el mismo token del cliente tras activarlo | 200; datos del usuario, rol Cliente y cuenta Activa. |
| Cliente activo intenta activar otra cuenta | 403. |
| Intento de activación sin token | 401. |

No se registran correos personales, contraseñas, hashes, tokens ni contenidos del almacén de secretos. Las comprobaciones antiguas de 0 usuarios/0 clientes describen el estado anterior a estas altas manuales, no el estado actual de la base. No se consultaron ni modificaron datos de las cuentas personales para documentar este cierre.

Revisión final: diff revisado y git diff --check sin errores; 103 archivos versionados/candidatos revisados y 0 preparados en el índice. No se detectaron indicadores de credenciales, claves privadas o tokens literales en los textos examinados, ni rutas de generados/secretos locales entre los candidatos. Se comprobaron exclusiones para bin, obj, .vs, resultados de pruebas, APK/AAB, .env.local y PFX. No se leyó el almacén local de secretos. Los hashes SHA-256 del PDF y los dos PNG originales coinciden con los iniciales. No se repitieron compilación ni pruebas automatizadas: esta actualización solo modifica documentación y conserva los resultados anteriores.

Bloque de autenticación de la API cerrado con pruebas automatizadas y manuales documentadas. Quedan fuera de este cierre la integración MAUI/API, pantallas de autenticación, CRUD del negocio y el pendiente visual V1. No iniciar otro bloque sin confirmación. No se hizo commit ni push. Mensaje de commit sugerido: `feat: agregar autenticacion y activacion de cuentas en la API`.

## 13. Bloque de autenticación de BanosQuimicos.App

### Autorización, punto de partida y límites

El estudiante confirmó commit, push y merge de autenticación API y master local actualizado. Se leyeron AGENTS.md, el plan completo, contratos/controladores y guías de API; se revisó visualmente la captura de Figma en la página 5 del PDF aprobado. Git estaba limpio en master 9234f4f, coincidente con la referencia local origin/master; se creó feature/autenticacion-app desde ese punto, sin descartar cambios. No se hizo un nuevo fetch/pull ni se alteraron documentos originales.

Se explicó antes de implementar: formularios Razor acordes con Figma y los campos confirmados; cliente HTTP y sesión separados de la vista; SecureStorage; consulta /yo como fuente del estado actual; conexión Windows HTTPS y Android Debug por loopback mediante USB; ajuste de área segura nativa. Sin CRUD, activación administrativa en la app, recuperación, proveedores externos ni cambios de esquema.

### Archivos y comportamiento implementado

- App/Autenticacion/: contratos con validación, ClienteAutenticacion y SesionUsuario. Registro con nombre, apellido, correo, teléfono y contraseña obligatorios; razón social, CUIT y dirección principal opcionales. No se envían rol ni estado. Correo recortado; unicidad sin mayúsculas validada por la API existente.
- App/Components/Autenticacion/: Login y Registro; Home.razor selecciona login/registro, aviso Pendiente, cuenta activa mínima con nombre/rol o estado sin verificar. Tras registro exitoso informa activación requerida y precarga correo en login, sin iniciar sesión automáticamente. Campos y botones se deshabilitan durante el envío; la sesión evita consultas simultáneas.
- El cliente usa POST /api/autenticacion/registro, POST /api/autenticacion/login y GET /api/autenticacion/yo reales. Solo /yo recibe Bearer, por petición. No se adjunta a un cliente global ni a URLs externas. El handler no sigue redirecciones. No se guardan contraseñas ni claves JWT en la app.
- /yo es obligatorio después del login, al restaurar, al volver de segundo plano y al actualizar. Pendiente conserva únicamente estado y aviso, sin perfil/rol operativo; Activa muestra identidad actual. Ante falta de conexión o 403 se oculta la identidad y se ofrece reintentar/cerrar. 401 de /yo elimina la sesión; login 401 informa credenciales incorrectas; login 403, cuenta no habilitada. La API no distingue en el 401 vencimiento de suspensión/baja; el mensaje lo refleja sin inventar una causa.
- App/Plataforma/: ConexionApi crea el HttpClient (20 segundos), AlmacenSesionSeguro conserva exclusivamente JWT con SecureStorage. Preferences contiene solo el indicador no sensible de restauración. Cerrar elimina memoria/token y deshabilita restauración. Si no se puede guardar, se informa sesión solo en memoria. No hay refresh token ni revocación global al cerrar sesión.
- MauiProgram y App registran servicios y consulta al reanudar. MainPage usa SafeAreaEdges=All; App.xaml aplica Resize del teclado Android. CSS permite desplazamiento natural, formularios sin alturas fijas y foco visible. Se conservan paleta/acciones de Figma, etiqueta Correo electrónico confirmada y cabecera textual BQ. No se presenta como copia exacta de un logotipo.
- AndroidManifest.Debug.xml y Desarrollo/network_security_config.xml permiten HTTP solo a 127.0.0.1 en Android Debug. El csproj selecciona el manifiesto antes de compilar, después de que los targets MAUI hayan elegido el base; se detectó y corrigió esa sobrescritura durante la revisión. Release usa el manifiesto base y excluye ese recurso. Windows usa https://localhost:7292; Android Debug usa http://127.0.0.1:5148 mediante adb reverse por USB. No hay callback que omita certificados ni permiso HTTP global. Release necesita configurar el servidor HTTPS real antes de distribuir.
- Pruebas: el proyecto existente enlaza el C# real de autenticación de la app (sin exigir runtime MAUI en los tests). AutenticacionAppPruebas simula transporte y almacén; una prueba nueva en AutenticacionPruebas usa ese cliente contra los controladores reales, SQL Server y activación administrativa.

### Verificaciones realizadas por el agente

- Suite completa: **29 pruebas correctas, 0 fallidas, 0 omitidas**: 16 del cliente/sesión, 12 previas de API y 1 integración nueva app/API. Comprueba rutas/token, estado actual frente al del login, ocultamiento de perfil Pendiente, restauración/cierre, pérdida de conexión/reintento, distinción 401/403/409/400/500/3xx, almacén fallido, respuesta malformada y prevención de consultas duplicadas. La integración nueva registra, ingresa como Pendiente, activa por administrador, actualiza con el mismo token y cierra.
- Integración ejecutada con TestServer y nombre normal de LocalDB, en una base nueva BanosQuimicosAuthPruebas_<GUID> conservada. Las credenciales se generan en memoria y las cuentas se identifican como PRUEBA AUTENTICACION/example.invalid. No se consultaron credenciales ni se modificaron cuentas personales de BanosQuimicosDb. Estas pruebas no equivalen a tráfico HTTPS real desde el teléfono.
- Primera compilación App Debug Android/Windows: correcta, 0 errores y 0 advertencias. La solución completa también compiló con 0 errores/advertencias. Tras el último ajuste de selección de manifiesto y ruta de recurso, se recompiló Android: 0 errores/advertencias. Se verificó en el manifiesto generado networkSecurityConfig y en res/xml el recurso con base HTTP denegado y excepción exclusiva 127.0.0.1. Release se evaluó sin ese manifiesto/recurso (no se ejecutó una compilación Release).
- Ejecución visual en Windows: se abrió la app nativa, se revisaron login y registro, se desplazó el formulario hasta sus últimos campos y botones, y se envió vacío comprobando mensajes de validación. No se introdujeron credenciales personales ni se registraron cuentas desde la interfaz. La inspección detectó un parámetro de correo mostrado literalmente y se corrigió; se reabrió la versión compilada y se comprobó el correo inicial vacío.
- ADB devices -l no encontró teléfonos conectados durante esta sesión. No se instaló ni ejecutó esta versión en Android. Se consultó la configuración evaluada de Release, que excluye el recurso de red de Debug.
- El primer ejecutor de pruebas en aislamiento no completó; la ejecución fuera de ese aislamiento pasó. No se presenta ese intento como una prueba de funcionalidad fallida ni como evidencia de éxito.

### Pruebas manuales: estado al cierre inicial

docs/autenticacion-app.md contiene pasos reproducibles para API HTTPS, Windows, túnel USB Android, registro, cuenta Pendiente, activación en Postman con IdUsuario obtenido por consulta de solo lectura, actualización a Activa y cierre. Incluye errores, vencimiento, reinicio y teclado/desplazamiento. README enlaza la guía y deja de afirmar que la app no consume la API.

Al cierre inicial quedaba pendiente que el estudiante repitiera el flujo completo en Windows y Android físico: certificado de desarrollo, transporte nativo, persistencia real SecureStorage, activación con sesión abierta, reinicio, vencimiento y cierre. La simulación de almacenamiento no verifica SecureStorage nativo. **V1 al cierre inicial: ajuste del título implementado; confirmación visual Android pendiente (resuelto posteriormente según el cierre final siguiente)**, junto con teclado abierto, campos inferiores, botones, rotación y barras del sistema. No dar por cerrado V1 solo por compilar. Las pruebas Windows/Android informadas en bloques anteriores correspondían a la pantalla inicial anterior.

No se agregaron tablas, migraciones, paquetes ni endpoints de negocio. No se inicia el siguiente bloque sin confirmación. Bloque implementado para revisión, con pruebas personales pendientes según lo anterior. No se hizo commit ni push. Mensaje sugerido: `feat: agregar autenticacion y gestion de sesion en la app MAUI`.

Revisión final de archivos: git diff --check correcto; 114 archivos versionados/candidatos, sin rutas de bin/obj/.vs/APK/AAB/temporales/secretos ni indicadores de claves privadas o tokens literales en los textos examinados. Índice sin archivos preparados. Exclusiones de generados y .env.local comprobadas; SHA-256 del PDF y los dos PNG originales sin cambios. No se leyó el almacén de secretos ni se hizo commit/push.

Conectividad local adicional: API ejecutada con perfil https y configuración normal; GET https://localhost:7292/api/estado y GET http://localhost:5148/api/estado devolvieron 200. HTTPS comprobado con validación normal del certificado, sin bypass y sin enviar credenciales. La primera consulta anterior al arranque no pudo conectar; luego del arranque fue correcta. Esto confirma los puertos locales para la guía, no una prueba de transporte desde Android ni un login completo desde la app Windows. Se detuvo la API de comprobación y se cerró la app usada para inspección para dejar libre el entorno.

### Cierre final para commit: pruebas manuales confirmadas el 28/10/2026

El estudiante confirmó personalmente resultados correctos en Windows y en su Moto G9 Plus físico conectado por USB. Estas verificaciones manuales son independientes de las **29 pruebas automatizadas previamente ejecutadas por el agente**; no se atribuyen a una nueva ejecución automática.

| Verificación manual | Windows | Moto G9 Plus / Android |
| --- | --- | --- |
| Campos obligatorios y credenciales incorrectas | Correcto | Correcto |
| Registro con opcionales vacíos y rechazo de correo duplicado | Correcto | Correcto |
| Cuenta Pendiente sin perfil ni rol | Correcto | Correcto |
| Activación desde Postman y actualización en la app sin otro login | Correcto | Correcto |
| Restauración de sesión tras cerrar y abrir la app | Correcto | Correcto |
| Pérdida de conexión: ocultar identidad y mostrar error | Correcto | Correcto |
| Recuperación de conexión al reiniciar la API | Correcto | Correcto |
| Cierre de sesión persistente tras volver a abrir la app | Correcto | Correcto |

Además, en Windows confirmó ventana reducida, tabulación y desplazamiento. En Android confirmó título completo, barras del sistema, teclado, desplazamiento y rotación. La conexión del teléfono funcionó mediante `adb reverse` al puerto 5148. **V1 cerrado por confirmación visual del estudiante en el dispositivo físico.** La restauración y el cierre persistente quedaron comprobados manualmente en ambas plataformas, además de las pruebas previas con almacén simulado.

**Pendiente: vencimiento real del token por transcurso del tiempo. No probado manualmente.** Los escenarios automatizados de token vencido/401 no sustituyen esta prueba real en la app. No se afirma haber probado otros escenarios manuales no informados.

Revisión para commit: rama feature/autenticacion-app; diff y archivos nuevos revisados, git diff --check sin errores. Se examinaron 114 archivos versionados/candidatos y el índice está vacío. No se detectaron rutas de generados/temporales/secretos ni indicadores de tokens literales, claves privadas o credenciales en los textos examinados. Se comprobaron exclusiones de bin, obj, .vs, TestResults, APK/AAB, temporales, .env.local y PFX. Los hashes SHA-256 del PDF y los dos PNG originales permanecen iguales. Se conserva código, solución, recursos y documentación necesarios; no se leyó el almacén de secretos ni se guardaron credenciales o tokens.

En esta actualización solo se modificó documentación: no se repitieron compilación ni tests, y se conservan sus resultados anteriores. Bloque de autenticación de la app **cerrado y listo para commit**, con el vencimiento real registrado como pendiente. No se hizo commit ni push ni se inició otro bloque. Mensaje sugerido: `feat: agregar autenticacion y gestion de sesion en la app MAUI`. Esperar confirmación antes del siguiente bloque.

## 14. Consulta y activación de cuentas desde la app del administrador

### Autorización y revisión previa

El estudiante confirmó commit, push y merge de autenticacion-app y master actualizado. Se revisaron AGENTS.md, el plan, contratos, controles de seguridad y documentación de API; texto de páginas 1-2 del PDF aprobado y captura Figma del administrador en página 6. Master estaba limpio en 6c73160, coincidente con la referencia local origin/master. Se creó feature/gestion-cuentas desde ese punto, sin descartar cambios, sin fetch/pull ni commit/push del agente.

Antes de modificar archivos se explicó: agregar listado/detalle protegidos a CuentasController, tarjetas en la app, activación mediante el POST existente con confirmación y lectura del estado actual ante conflicto. No se necesitó modificar el modelo. Figma permite reconocer Clientes y Empleados, tarjetas, paleta y navegación; no todos los textos pequeños son legibles. La búsqueda, filtro, paginación, detalle y confirmación son adaptaciones al alcance autorizado, no controles afirmados como visibles en la captura. No se agregan los controles de baja/cambio de rol que pertenecen a otro bloque.

### Implementación y archivos

- API/Contratos/Cuentas/ConsultaCuentas.cs: parámetros validados y DTOs explícitos para listado/detalle. API/Controllers/CuentasController.cs: GET /api/cuentas y GET /api/cuentas/{id}, bajo la política Administrador ya existente. Se conserva POST /api/cuentas/{id}/activar con UPDATE condicional y respuestas 204/409/404.
- Listado: página desde 1, tamaño predeterminado 10 y máximo 50; búsqueda opcional hasta 254 caracteres recortados por nombre/apellido/correo, combinada con estado Pendiente/Activa/Suspendida/BajaLogica. Filtra, cuenta y pagina en SQL mediante EF AsNoTracking. Orden apellido/nombre/IdUsuario para desempatar. Rechaza parámetros fuera de rango y desplazamientos que excedan int.MaxValue. Una página fuera de resultados devuelve lista vacía con el total filtrado.
- DTOs exponen solo IdUsuario, nombre, apellido, correo, teléfono, rol, estado, fecha de registro y PuedeActivar. La fecha se presenta en hora local en la app. PuedeActivar exige Cliente/Pendiente con perfil, sin reemplazar las validaciones del POST. No devuelve entidades EF, contraseñas, hashes ni tokens.
- App/Autenticacion/ContratosCuentas.cs y ClienteAutenticacion.cs: contratos y peticiones a rutas fijas de nuestra API; Bearer por petición, también para las nuevas rutas administrativas, sin exponer el token a las vistas. Diferencia 409 de activación respecto del correo duplicado de registro, y trata 404 como cuenta no disponible.
- SesionUsuario.cs: operaciones administrativas serializadas con la sesión actual, sin cambiar SecureStorage ni URL/handler. Al recibir 401 limpia sesión; ante 403 o falta de conexión oculta identidad administrativa y exige nueva consulta, conservando las reglas anteriores. La API vuelve a validar rol/estado actuales; no depende de la interfaz.
- Home.razor muestra Gestionar cuentas únicamente a un administrador activo. Components/Cuentas/GestionCuentas.razor contiene listado y detalle con tarjetas, formulario de búsqueda/filtro, controles de página, carga, vacío, error y regreso. CSS conserva la paleta y permite desplazamiento. No se agregan rutas navegables que eludan la selección por sesión.
- La activación requiere confirmación explícita de nombre/correo; cancelar no envía POST. Controles deshabilitados durante la operación. Tras 204 se consulta detalle/listado. Ante 409 se informa cambio en otra sesión y se vuelve a leer el estado sin repetir el POST. Si la última página filtrada queda vacía, se ajusta a la última página válida. Si falla la lectura posterior, se informa el error y se puede volver a consultar; no se promete que una operación con respuesta perdida no se haya realizado.
- GestionCuentasPruebas.cs agrega pruebas de integración con la infraestructura aislada existente. AutenticacionAppPruebas.cs ajusta el caso simulado de conflicto de login para no confundirlo con duplicado de registro.

### Verificaciones realizadas por el agente

- API compilada inicialmente sin errores/advertencias. Solución completa compilada en Debug para API, pruebas, app Windows y app Android: **0 errores y 0 advertencias**. No se instalaron paquetes ni workloads nuevos.
- Suite final: **45 correctas, 0 fallidas, 0 omitidas**: 29 previas y 16 casos nuevos de gestión. La primera ejecución con 43 casos pasó y señaló dos advertencias de estilo xUnit; se corrigieron y se incorporaron dos pruebas adicionales antes de ejecutar la suite final sin esas advertencias.
- Permisos de listado/detalle/activación: anónimo 401; cliente activo, empleado activo y cuenta Pendiente 403; administrador suspendido o dado de baja después de emitir el token 401. Los intentos rechazados no activan el objetivo.
- Paginación: 23 cuentas de prueba con nombres iguales, cuatro páginas de tamaño 7, sin duplicados, orden repetible por IdUsuario, total consistente y página posterior vacía. Rechazo de página 0, tamaños 0/51, desplazamiento excesivo y estado desconocido.
- Búsqueda por nombre, apellido y correo (con espacios exteriores y mayúsculas en correo); filtros combinados Pendiente/Activa/Suspendida, sin coincidencias y carácter % literal. El filtro BajaLogica está implementado; no se afirma una prueba visual de ese filtro ni la creación de bajas desde la app.
- Detalle con conjunto exacto de campos permitidos, lista sin hash/contraseña/token, detalle inexistente 404. Activación simultánea desde dos clientes: un 204 y un 409; repetición 409, estado actual Activa, indicador PuedeActivar falso y un solo perfil cliente.
- Cliente C# real de la app contra TestServer/SQL: sesión administrador, búsqueda, detalle pendiente, activación, repetición como conflicto, detalle Activa y desaparición del filtro Pendiente. Cambio de rol en datos aislados después del login: API rechaza con 403, sesión oculta identidad administrativa y /yo posterior refleja Cliente.
- Cada clase de integración usa una base nueva BanosQuimicosAuthPruebas_<GUID>, por nombre normal de LocalDB; se conserva sin borrar bases ni registros anteriores. Cuentas identificadas PRUEBA CUENTAS/example.invalid y secretos aleatorios solo en memoria. No se ejecutaron altas, activaciones o cambios de nuestras cuentas existentes en BanosQuimicosDb. Las modificaciones de rol/estado para pruebas ocurrieron exclusivamente en bases aisladas; no agregan endpoints de edición.
- El intento MAUI dentro del aislamiento falló sin diagnóstico de código; se repitió fuera del aislamiento y compiló. La consulta inicial ADB aislada no pudo iniciar su daemon; fuera del aislamiento detectó Moto G9 Plus conectado como device. No se cambió el túnel, firewall ni configuración de red.

### Pruebas manuales pendientes y reproducción

Guía nueva docs/gestion-cuentas.md con rutas, filtros, códigos HTTP y pasos en Visual Studio/Windows y Moto G9 Plus por USB. README y docs/autenticacion-api.md enlazan el bloque. Se conserva la preparación HTTPS/adb reverse 5148 y las reglas de sesión anteriores.

**Estas pantallas nuevas no fueron ejecutadas ni probadas manualmente por el agente en Windows o Android.** La compilación y TestServer no sustituyen confirmar carga, vacío, búsqueda/filtro/páginas, detalle, cancelar/confirmar activación, conflicto entre dos instalaciones, errores, teclado/desplazamiento y rotación. Las pruebas manuales anteriores del estudiante corresponden a autenticación, no a gestión de cuentas. No se solicitan credenciales en el chat. **El vencimiento real del token continúa pendiente y no se marca como probado.** V1 del título permanece cerrado por la confirmación previa del estudiante; verificar que no regrese en las pantallas nuevas.

### Revisión de Git y cierre

Se revisaron diff y archivos nuevos. git diff --check correcto; 119 archivos versionados/candidatos y ninguno preparado en el índice. No se detectaron rutas de generados/temporales/secretos ni indicadores de credenciales, tokens literales o claves privadas en los textos examinados. Se comprobaron exclusiones de bin, obj, .vs, TestResults, APK/AAB, temporales, .env.local y PFX. Los hashes SHA-256 del PDF y ambos PNG originales permanecen iguales. No se leyó el almacén de secretos. No se modificaron modelos, migraciones ni configuración de conexión Windows/Android, certificados o firewall.

Bloque **cerrado para revisión**, con pruebas manuales pendientes según la guía. Se conservan las 12 tablas del negocio y el historial EF. Sin edición, creación de empleados, suspensión, baja, cambios de rol, pagos ni facturación. No se hizo commit ni push ni publicación, ni se inició otro bloque. Mensaje sugerido: `feat: consultar y activar cuentas desde la app del administrador`. Esperar confirmación antes de continuar.

## 15. Ajuste puntual del destino de la app para pruebas HTTPS (03/11/2026)

El estudiante informó que resolvió el alta del primer administrador por su cuenta y que la API ya está publicada en https://celeris.runasp.net/. Autorizó el cambio mínimo de conexión de la app, conservando opción local, HTTPS, sesiones separadas por destino y el trabajo/PR de feature/gestion-cuentas. No solicitó nuevas pantallas ni cambios al Figma.

Se releyeron AGENTS.md, configuración de conexión, almacén seguro y plan. La rama continúa feature/gestion-cuentas. Al comenzar, el único candidato no versionado era scripts/publicacion/; se conservó intacto. No se cambió de rama ni se realizaron operaciones sobre el PR, commit o push.

Cambios: ConexionApi.cs incorpora UsarHosting=true para seleccionar https://celeris.runasp.net/ también en Debug, tanto Android como Windows. Cambiar a false y recompilar conserva Windows HTTPS localhost:7292 y Android Debug loopback:5148 por adb reverse. El HttpClient mantiene certificado validado normalmente, redirecciones deshabilitadas y tiempo de espera de 20 segundos. No se cambiaron manifiestos, permisos de red, API ni pantallas.

AlmacenSesionSeguro.cs recibe el mismo HttpClient de DI y asocia la sesión a su URL base mediante un valor no sensible en Preferences. Antes de restaurar, si la URL no coincide o la sesión antigua no tiene URL asociada, deshabilita restauración y elimina el token previo, sin leerlo/enviarlo a otro servidor. Al guardar, conserva JWT en SecureStorage y URL en Preferences. Cambiar efectivamente de servidor requiere otro login; no conserva sesiones paralelas por destino. Si falla limpiar el almacén se mantiene el manejo de errores existente y no se recupera el token anterior. No guarda contraseñas ni claves JWT.

La guía docs/autenticacion-app.md añade el destino seleccionado, ejecución Android/Windows, selección local y comprobación manual del cambio de servidor. Para hosting: teléfono con Internet, app Debug en Visual Studio y F5; no requiere iniciar API local ni adb reverse. La depuración USB sigue disponible para instalar/depurar.

Verificaciones: 16 pruebas automatizadas previas de cliente/sesión correctas, sin bases de datos ni altas. No prueban el SecureStorage nativo ni la nueva comparación de URL: esta última fue revisada en código y requiere comprobarse en los dispositivos. GET público https://celeris.runasp.net/api/estado devolvió 200 con el mensaje esperado y validación normal del certificado; no se enviaron credenciales ni se modificaron cuentas. La consulta inicial aislada no resolvió DNS y se repitió correctamente fuera del aislamiento. La compilación MAUI aislada falló sin diagnóstico de código y se repitió fuera de ese entorno.

Pendientes manuales: iniciar sesión en hosting, cerrar/reabrir, cambiar a local y comprobar login nuevo sin restaurar sesión del hosting, y regresar al hosting con nuevo login. Probar el recorrido de gestión pendiente en Android y activación desde dos sesiones. El estudiante informó que en Windows gestión de cuentas llegó hasta desconexión/recuperación correctas; esto no prueba los casos pausados. **Vencimiento real del token: pendiente, no probado.** No se modificó Figma; si falta contexto para otra pantalla, pedirlo antes de crearla.

Resultado final: app compilada en Debug para Windows y Android, 0 errores y 0 advertencias. Diff revisado y git diff --check correcto; índice vacío y sin indicadores de claves privadas/tokens ni rutas de generados/secretos entre los candidatos examinados. Solo se modificaron dos archivos de plataforma y dos documentos; el script SQL previo quedó intacto. No se ejecutó esta versión de la app en los dispositivos ni se probaron credenciales contra el hosting. Ajuste cerrado para revisión. Mensaje sugerido: `fix: seleccionar hosting HTTPS y aislar sesion por servidor`. Sin commit ni push. Esperar confirmación antes de otro bloque.

### Cierre complementario: pruebas manuales confirmadas en gestión y hosting (03/11/2026)

El estudiante confirmó personalmente las siguientes pruebas. Son verificaciones **manuales informadas por él**, separadas de las 45 pruebas automatizadas de gestión/autenticación y de las 16 pruebas de regresión ejecutadas previamente para el ajuste de conexión. No son nuevas ejecuciones del agente.

| Plataforma / escenario | Resultado manual confirmado |
| --- | --- |
| Windows: listado, búsqueda y filtros | Funcionaron correctamente. |
| Windows: detalle de cuenta | Funcionó correctamente. |
| Windows: cancelar la confirmación de activación | Funcionó correctamente. |
| Windows: pérdida de conexión y recuperación | Recuperación correcta. |
| Moto G9 Plus contra https://celeris.runasp.net/: activar cliente | Activación realizada correctamente desde la app. |
| Windows contra la API alojada: intentar activar también la cuenta activada desde Android | La app informó el cambio y mostró la cuenta Activa. Conflicto entre dos sesiones comprobado manualmente. |
| Android: cerrar sesión del administrador y reabrir la app | Apareció el login; cierre persistente confirmado. |
| Android: ingresar como cliente | Mostró su rol sin acceso a Gestionar cuentas. |

Estas pruebas actualizan los pendientes históricos de las secciones 14 y 15: la activación desde Android, el manejo del cambio desde otra sesión y el cierre persistente del administrador en Android ya están confirmados. No se marcan como probados otros recorridos visuales, de paginación o de interacción no informados en este mensaje.

**Siguen pendientes:** vencimiento real del token por transcurso del tiempo y prueba de alternar entre servidores en el dispositivo (hosting → local → hosting), comprobando que no se restaure ni envíe una sesión del servidor anterior. El cierre y reapertura en el mismo hosting no sustituyen esa prueba de cambio de destino.

Regla de pantallas confirmada y registrada en AGENTS.md: deben corresponder al Figma aprobado. La pantalla adicional aceptada para gestionar cuentas es la única excepción; no autoriza nuevas pantallas fuera de Figma. Si falta una referencia, pedir la captura antes de implementar. No se modificaron pantallas en esta actualización.

Revisión previa al commit: se examinaron 120 archivos versionados/candidatos; no se detectaron perfiles de publicación, credenciales, claves privadas, tokens literales ni rutas de generados entre los candidatos examinados. Se comprobaron exclusiones para .pubxml, .pubxml.user, .publishsettings, .env.local, PFX, bin/obj y .vs. Índice vacío y git diff --check sin errores. No se abrieron perfiles de publicación ni almacenes de secretos. El script SQL previo y los cambios de conexión/sesión existentes se conservan sin modificaciones en esta ronda.

Solo se actualizaron AGENTS.md y este plan; no se repitieron compilación ni pruebas automatizadas por tratarse de documentación. Bloque cerrado para revisión/commit con los pendientes explícitos anteriores. Mensaje sugerido: `docs: registrar pruebas manuales y regla de pantallas Figma`. No se hizo commit ni push ni se inició otro bloque. Se conserva feature/gestion-cuentas y el PR existente sin operaciones remotas.

## 16. Acceso de empleados por cambio de rol (feature/alta-empleados)

### Preparación inicial y corrección de alcance

El estudiante confirmó merge de gestión de cuentas y del script de publicación, pull de master y compilación local correcta; informó acceso al hosting y activación desde dos sesiones en Windows/Moto G9 Plus. Permanecen pendientes el vencimiento real del token y alternar servidores en el dispositivo. Estos resultados son manuales comunicados por el estudiante.

Revisión inicial: master limpio en cd30fed, coincidente con origin/master local. Se creó feature/alta-empleados sin descartar cambios. Se releyeron AGENTS.md, plan, registro/login actuales y capturas de Figma del administrador y empleado. La interpretación inicial de un alta administrativa fue corregida posteriormente por el estudiante: el registro público conserva Cliente/Pendiente y el administrador cambia el rol de esa misma cuenta. No se agregaron entidades, endpoints, pantallas ni migraciones durante esta preparación.

Figma: página 5 muestra inicio de empleado con saludo, referencia a tareas, acceso a tareas asignadas y cerrar sesión. El número de tareas mostrado es un ejemplo; no se tomará como dato real. Página 6 muestra Clientes y Empleados del administrador, pero no se identificó un formulario de alta de empleado ni su entrada. En esa revisión se pidió la referencia del supuesto formulario; el estudiante aclaró que ese formulario no existe y aportó la vista correcta para cambio de rol. Gestión de cuentas continúa siendo la única excepción visual autorizada.

Dudas planteadas en la preparación inicial, **superadas por la corrección posterior y no aprobadas**: referencia visual del alta y su acceso; estado inicial del empleado (propuesta Activa, por alta administrativa); asignación de contraseña inicial (propuesta que el administrador la ingrese y la entregue fuera de la app, sin cambio obligatorio en este bloque). Son propuestas, no reglas confirmadas.

Avance independiente: ConexionApi.cs selecciona UsarHosting=false para este bloque local: Windows https://localhost:7292, Android Debug http://127.0.0.1:5148 mediante adb reverse. appsettings.Development.json conserva LocalDB/BanosQuimicosDb. Antes de ejecutar habrá que comprobar que ninguna configuración externa reemplace la conexión por la alojada. No se consultó ni modificó MonsterASP. El cambio de destino invalida la restauración de la sesión anterior según la lógica existente; su comprobación manual en dispositivo sigue pendiente.

No se ejecutaron compilaciones/tests en esta preparación ni se creó una cuenta. La respuesta posterior reemplazó la propuesta de alta directa por el cambio de rol descrito a continuación. Con el modelo actual, una cuenta de empleado cabe en USUARIOS/ROLES sin migración prevista; los futuros endpoints necesitarán publicación posterior para funcionar en hosting, que no se realizará en este bloque. Sin commit ni push.

### Alcance corregido y confirmado

El estudiante indicó la página 1 del PDF aprobado y adjuntó la captura ampliada de Clientes y Empleados (página 6): la persona se registra desde la app como cualquier usuario, inicia Cliente/Pendiente y conserva su contraseña. No hay formulario de alta de empleado, contraseña entregada por administrador ni creación de otra cuenta. Solo un administrador activo puede cambiar Cliente ↔ Empleado desde la cuenta existente. **Rol y estado son decisiones separadas: cambiar rol no activa la cuenta.** El recorrido confirmado activa primero al cliente con el endpoint existente y luego le asigna Empleado.

Se inspeccionó la captura adjunta: se distinguen las pestañas Cliente/Empleado y un desplegable por tarjeta; los valores pequeños de ejemplo no se toman como reglas nuevas. El alcance solicitado define las dos opciones del selector. Se explicaron antes de modificar código la implementación dentro de la vista existente y el inicio de empleado de página 5. No se agregó otra pantalla administrativa; Gestión de cuentas sigue siendo la única excepción visual autorizada. El inicio Empleado ya tiene referencia aprobada.

### Implementación y archivos

- API/Contratos/Cuentas/ConsultaCuentas.cs: agrega filtro opcional Rol=Cliente/Empleado y CambioRolSolicitud con RolAnterior/NuevoRol, ambos requeridos y limitados a esas dos opciones. Rechaza campos JSON adicionales, incluido estadoCuenta.
- API/Controllers/CuentasController.cs: PUT /api/cuentas/{id}/rol, bajo la política Administrador activo. Consulta ID del rol destino del catálogo existente y actualiza únicamente USUARIOS.IdRol, condicionado al rol anterior. No modifica usuario, contraseña/hash, perfil cliente, estado, fecha de registro ni referencias históricas. No permite otorgar Administrador ni modificar una cuenta Administrador; 204 éxito, 400 datos inválidos/mismo rol, 404 inexistente y 409 cambio concurrente o cuenta no compatible.
- GET /api/cuentas admite filtro por rol en SQL, combinado con búsqueda, estado y paginación. Omitirlo conserva la consulta general de API y su contrato previo.
- App/Autenticacion/ClienteAutenticacion.cs y SesionUsuario.cs: petición de cambio con Bearer por solicitud a la misma API, protección local de administrador y manejo de conflicto genérico sin confundirlo con correo duplicado. Se conservan 401/403, consulta actual de /yo, ocultamiento de identidad ante falta de conexión y SecureStorage asociado al destino.
- Components/Cuentas/GestionCuentas.razor: vista Clientes y Empleados con pestañas, selector Cliente/Empleado por tarjeta, confirmación/cancelación y recarga del listado tras cambio o conflicto; sin repetir automáticamente la escritura. Al cambiar de rol la cuenta pasa a la otra pestaña; se ajusta paginación al quedar vacía la última página. Se preserva el detalle y activación existente. No se agregan alta de cuentas, edición, suspensión ni baja.
- Components/Empleados/InicioEmpleado.razor y Home.razor: Empleado activo recibe inicio correspondiente a página 5, con saludo, rol, Tareas asignadas y cerrar sesión; no ve gestión administrativa. Tareas asignadas queda deshabilitado y explicado hasta su bloque funcional, sin cantidad ficticia ni tareas inventadas. Los estilos conservan la paleta, saludo amarillo y acceso verde de la referencia.
- Se mantiene UsarHosting=false para local. docs/acceso-empleados.md contiene contratos y guía Windows/Android local; README y docs/autenticacion-app.md señalan el destino actual.

La activación existente sigue limitada a Cliente/Pendiente: no se amplió silenciosamente. Un Pendiente cambiado a Empleado sigue restringido; para usar la activación existente debe volver a Cliente. El recorrido principal confirma el orden activar → cambiar rol. Cambiar de rol una cuenta Activa/Suspendida/BajaLogica tampoco modifica su estado.

### Verificaciones ejecutadas

- **60 pruebas automatizadas correctas, 0 fallidas y 0 omitidas**: 45 anteriores y 15 casos nuevos en CambioRolPruebas.cs. Cada clase usa su base nueva BanosQuimicosAuthPruebas_<GUID> en LocalDB; se conservan los datos, sin borrar bases ni tocar cuentas existentes.
- Recorrido con el cliente C# real de la app y controladores reales en TestServer: registro Cliente/Pendiente, aviso restringido, activación por administrador, cambio a Empleado, cierre y login con el mismo correo/contraseña, /yo con mismo IdUsuario y rol Empleado. Vuelta a Cliente reflejada por /yo y filtros de rol.
- Se comprobó conservación del hash, correo, fecha de registro, IdCliente, razón social, CUIT y dirección, un único perfil y una tarea realizada que mantiene IdEmpleado al volver a Cliente. Esa tarea y baño se crearon exclusivamente como datos de prueba aislados; no implementan endpoints ni pantallas de tareas.
- Teorías de estado Pendiente/Activa/Suspendida/BajaLogica: cambio ida/vuelta conserva estado. Empleado Pendiente solo obtiene aviso, sin rol operativo, y falla acceso a operación/gestión. Anónimo 401; cliente activo, empleado y pendiente 403; administrador suspendido o dado de baja con token previo 401. Intentos rechazados no cambian el objetivo.
- Rol Administrador/inventado/vacío, estado extra y mismo rol se rechazan; cuenta Administrador protegida. Dos cambios concurrentes con el mismo rol anterior: uno 204 y otro 409. Repetición 409 y usuario inexistente 404. No hay rowversion: no se promete detectar una ida/vuelta completa que termine en el mismo rol anterior.
- Se corrigió un error de alias de namespace del archivo nuevo de pruebas antes del resultado final. La compilación MAUI aislada volvió a fallar sin diagnóstico y se repitió fuera del aislamiento. El empaquetador Android D8 falló por memoria nativa de Java según su informe local; se diagnosticó y se probó configuración temporal de menor consumo sin modificar el proyecto. Resultado definitivo de compilación en el cierre siguiente.
- EF dbcontext info, con conexión de proceso explícita, mostró BanosQuimicosDb y (localdb)\MSSQLLocalDB; migrations has-pending-model-changes indicó que no hay cambios desde la migración inicial. No se modificaron entidades, configuraciones EF ni migraciones: permanecen las 12 tablas más historial técnico.
- API local arrancada con conexión explícita a LocalDB, perfil https y puertos localhost 7292/5148. GET /api/estado devolvió 200 y PUT de rol sin token 401, sin cambios en BanosQuimicosDb. Se detuvo la API al terminar para liberar puertos. No se contactó ni modificó el hosting o db77593.

### Pruebas manuales y publicación pendientes

Seguir docs/acceso-empleados.md: fijar conexión local en una terminal nueva, revisar DbContext, iniciar API en esa terminal, verificar BaseAddress de la app en depurador y seleccionar Debug/Windows o Moto G9 Plus con adb reverse 5148. Probar registro, aviso Pendiente, activación, selector/cancelación/confirmación de rol, login con misma contraseña e inicio Empleado; vuelta a Cliente y rechazos administrativos. **TestServer y compilación no equivalen a prueba visual o interacción nativa**; el estudiante debe confirmar estos controles nuevos en ambas plataformas. No se usaron sus credenciales para automatizar la interfaz.

Continúan pendientes el vencimiento real del token y alternar servidores en el dispositivo. No se publica esta función: una publicación futura de API y distribución de app serán necesarias para usar el endpoint nuevo en MonsterASP. No se requiere migración. No se hizo commit ni push; se conserva la rama feature/alta-empleados.

### Cierre del bloque corregido

Compilación Windows: correcta, 0 errores/advertencias. Android: correcta, 0 errores/advertencias, con parámetros temporales JavaMaximumHeapSize=512M y JavaOptions=-XX:+UseSerialGC, tras confirmar en el informe JVM que D8 fallaba por falta de memoria nativa. No se cambió el csproj, herramientas ni configuración global; la guía incluye el comando reproducible. El resultado de los 60 tests corresponde al código corregido; no se presenta el intento inicial con alias erróneo como ejecución satisfactoria.

Diff y archivos nuevos revisados; git diff --check sin errores. 123 archivos versionados/candidatos, índice vacío, sin indicadores de secretos ni perfiles de publicación o generados entre los candidatos de texto examinados. Los informes Java quedaron ignorados como logs locales. PDF/PNG originales con SHA-256 sin cambios. No hay cambios en entidades, migraciones, manifiestos o almacenamiento de sesión; el cambio de destino local ya autorizado se conserva.

Bloque cerrado para revisión con el alcance corregido: **cambio de rol de cuentas existentes**, no alta administrativa. No se hizo commit, push o publicación ni cambios en db77593. Pendiente confirmar interacción nativa de los controles nuevos y los pendientes históricos expresos. Mensaje sugerido: `feat: cambiar roles Cliente y Empleado y mostrar inicio de empleado`. Esperar confirmación antes del siguiente bloque.

Confirmación final: la solución completa también compiló con 0 errores y 0 advertencias usando los parámetros temporales de Java ya documentados. Se actualizaron las referencias de README y la guía de gestión para que no sigan afirmando que el cambio de rol está ausente. No se repitieron tests después de esos ajustes exclusivamente documentales.

### Bloque reabierto: discrepancia de rol informada en prueba manual

El estudiante corrigió el resultado manual: en Gestión de cuentas seleccionó/cambió Cliente a Empleado y vio Empleado en la tarjeta, pero al ingresar con la misma cuenta continuó como Cliente. **Prueba manual fallida; bloque reabierto.** Los resultados automatizados anteriores no sustituyen esta prueba ni permiten dar por resuelto el recorrido nativo. Se investigará por el mismo IdUsuario: rol persistido en LocalDB, contrato real de login, /yo y rol que usa Home.razor para elegir inicio. Sin publicación ni cambios de hosting.

### Causa reproducida y corrección de la confirmación

El estudiante aclaró que **solo eligió Empleado en el desplegable**, sin pulsar Confirmar cambio de rol. Se reprodujo esa etapa: seleccionar modifica únicamente el valor propuesto en memoria; no hay PUT y SQL conserva Cliente. El desplegable mostraba Empleado y la confirmación aparecía arriba de la página, separada de la tarjeta. La diferencia estaba en la presentación del cambio pendiente, antes de la persistencia, no en una discrepancia entre SQL, login y /yo después de un guardado confirmado.

Corrección en GestionCuentas.razor: mostrar «Rol guardado» separado del selector; etiquetar que requiere confirmación; mostrar «Cambio sin guardar» y los botones Confirmar/Cancelar dentro de la misma tarjeta. Después del PUT se consulta de nuevo el detalle y solo se informa éxito si el rol devuelto coincide con el destino. Se mantiene la confirmación, las reglas de rol/estado y la interfaz existente, sin pantallas nuevas ni cambios del Figma original.

Nueva prueba Seleccionar_no_guarda_confirmar_persiste_y_login_abre_inicio_empleado: compila y renderiza los Razor reales de gestión, Home y sus componentes con HtmlRenderer de ASP.NET Core. No agrega paquetes: el proyecto de pruebas usa SDK Razor y enlaces a esos fuentes, conservando sus pruebas HTTP previas y el runtime net10.0 sin MAUI. Un activador de componentes de prueba permite invocar los callbacks reales y renderizar de nuevo en el dispatcher; no es una prueba de eventos de navegador ni automatización nativa.

Reproducción aislada correcta: **IdUsuario=2** en una base nueva BanosQuimicosAuthPruebas_<GUID>. Selección sin confirmar: SQL=Cliente y /yo de un login nuevo=Cliente; cancelación retira el aviso pendiente. Tras confirmar: SQL=Empleado, login nuevo con sub=2 (sin rol en su contrato), /yo=Empleado, SesionUsuario.Cuenta.Rol=Empleado y Home renderiza InicioEmpleado con «Rol: Empleado» y «Tareas asignadas», sin «Gestionar cuentas». Se conservaron correo/contraseña; no se imprimieron tokens ni claves. No se leyó ni alteró la cuenta personal usada por el estudiante.

La ejecución completa inicial de esta ronda falló en constructores de fixtures por conexión a LocalDB, antes del recorrido. Se verificó la instancia existente Running, sin borrar, reiniciar ni reparar bases. La regresión aislada pasó (1/1). Se repitió la suite sin compilar Android al mismo tiempo; su resultado se registra en el cierre. El comando EF de diagnóstico confirmó configuración local BanosQuimicosDb/(localdb)\MSSQLLocalDB; el test usa explícitamente su propia base nueva. No se contactó el hosting.

App Debug Windows y Android compiladas con 0 errores/advertencias; Android mantiene los parámetros de memoria temporales documentados, sin cambios permanentes de proyecto. Se actualizó la guía con el paso exacto de confirmación. **Pendiente repetir la prueba manual corregida en Windows y Android:** la prueba original permanece registrada como fallida; el nuevo resultado automatizado no se presenta como confirmación manual del estudiante.

Resultado final de esta corrección: suite completa **61 correctas, 0 fallidas, 0 omitidas**, después de comprobar LocalDB y terminar la compilación paralela. Se reforzó la regresión con la aserción explícita «Rol guardado: Cliente» y ausencia de «Rol guardado: Empleado» mientras el cambio está pendiente; se ejecutó de nuevo esa prueba y pasó (1/1). Windows y Android compilados con 0 errores/advertencias, sin migración ni publicación. Los fallos de conexión iniciales quedan registrados como intentos fallidos de infraestructura, no se atribuye una causa más precisa sin evidencia.

Revisión final: git diff --check correcto, 123 candidatos examinados sin indicadores de secretos, perfiles de publicación ni generados; índice vacío. Los resultados TRX quedan en tmp ignorado y no se versionan. Cambios anteriores de feature/alta-empleados conservados, sin commit ni push. La corrección está lista para revisión, pero el **bloque permanece reabierto hasta repetir la prueba manual corregida**. No se marca como superada la prueba manual original ni los pendientes de vencimiento/alternancia de servidores. Mensaje sugerido: `fix: distinguir rol pendiente y confirmar el cambio en la tarjeta`.

### Cierre final: prueba manual Windows aprobada (03/11/2026)

El estudiante confirmó personalmente que, después de **confirmar** el cambio Cliente → Empleado desde Gestión de cuentas, la cuenta apareció en el filtro Empleado. Al cerrar y volver a ingresar con las mismas credenciales, se mostró el inicio de empleado. **Prueba manual de Windows aprobada.** Este resultado corresponde a la repetición corregida; se conserva el registro histórico de la prueba fallida y su causa.

Esta confirmación manual es independiente de las 61 pruebas automatizadas y de las compilaciones previamente documentadas; no se ejecutaron nuevamente en esta actualización, que solo modifica el plan. No se registraron credenciales ni tokens.

**Bloque de acceso de empleados por cambio de rol cerrado**, por indicación del estudiante, con estos pendientes explícitos:

- Prueba manual del recorrido corregido en Android/Moto G9 Plus.
- Vencimiento real del token.
- Alternar servidores en el dispositivo y comprobar aislamiento de sesión.

No se publica la función ni se hacen cambios en MonsterASP. Se conservan los cambios existentes de feature/alta-empleados; no se hizo commit ni push ni se inició otro bloque. Verificación documental: git diff --check sin errores. Mensaje de commit sugerido para el bloque: `feat: cambiar roles Cliente y Empleado y mostrar inicio de empleado`.

## 17. Tareas asignadas del empleado: consulta local

### Punto de partida y alcance

El estudiante informó merge/pull del cambio de rol. Se revisaron AGENTS.md, PDF aprobado (texto funcional y referencia de página 5), la captura ampliada adjunta, UML y entidades/configuraciones existentes. Master estaba limpio en bc70d33, coincidente con la referencia local origin/master. Se creó feature/tareas-asignadas-empleado desde ese punto sin descartar cambios. No se hizo fetch/pull adicional ni operaciones de publicación.

Se explicó antes de implementar el recorrido Inicio → Tareas asignadas → Ver detalle, con regreso por las mismas vistas. Solo consulta: listado con tipo/baño/dirección/fecha y detalle con estado/tipo/cliente/baño/dirección/fecha. Datos de Figma tratados como ejemplos. No se inventaron usuarios, tareas, nombres ni cantidades para rellenar la interfaz.

El texto permite registrar trabajo, observaciones, reposición y marcar realizada; el UML incluye actualizar estado al registrar entrega/retiro/limpieza/mantenimiento. Ninguna referencia revisada define qué guarda cada uno de los dos botones de Figma ni muestra su formulario/confirmación. Registrar Trabajo y Actualizar Estado se conservan visibles y deshabilitados. La única decisión pendiente de esas acciones es cómo repartir y confirmar los datos entre ambos botones; si requieren otra pantalla se pedirá su captura aprobada antes de crearla. No se implementó borrado por el icono de ejemplo de Figma.

### Implementación

- API/Controllers/TareasController.cs y Contratos/Tareas/: GET /api/tareas/mias/resumen, GET /api/tareas/mias y GET /api/tareas/{id}. Política Empleado/Activa agregada en Politicas/ConfiguracionSeguridad. La consulta SQL siempre filtra por IdEmpleado obtenido del usuario autenticado; no se acepta elegir empleado por parámetro. Detalle ajeno e inexistente responden 404 sin revelar existencia.
- DTOs explícitos: IdTarea, tipo, código de baño, dirección, FechaProgramada, estado y nombre de cliente. No se serializan entidades, hashes, contraseñas, contacto del cliente ni observaciones privadas.
- Hoy se calcula en la API con reloj TimeProvider (registrado en Program.cs), día UTC−3 de Argentina, usando FechaProgramada >= inicio y < día siguiente. Cuenta todas las tareas propias programadas ese día, en cualquier estado; no equivale a tareas pendientes. Sin programación no suma. El listado es de todas las asignadas, ordenadas por fecha/IdTarea y con fechas nulas al final; no se lo limita a hoy.
- Dirección tomada exclusivamente de la ubicación vinculada a la tarea, preservando contexto histórico. Cliente viene del servicio de la tarea. Mantenimiento sin servicio o ubicación admite valores nulos; la app muestra el dato ausente sin inventar dirección ni sustituir la programación por fecha de registro.
- App/Autenticacion/ContratosTareas.cs, ClienteAutenticacion.cs y SesionUsuario.cs incorporan las consultas autenticadas, conservando token por petición y gestión de errores. El acceso serializado por rol se comparte con administración; 401 invalida sesión y 403/desconexión ocultan la identidad según el manejo anterior.
- Components/Empleados/InicioEmpleado.razor consulta contador real, habilita Tareas asignadas y conserva cierre de sesión. TareasAsignadas.razor contiene listado y detalle con carga, vacío, error/reintento, fecha legible y navegación Volver según Figma. Home.razor integra el recorrido dentro de la sesión Empleado, sin nuevas pantallas fuera de las referencias. Al perder rol/sesión se abandona la vista de tareas. Estilos conservan paleta y tarjetas.
- No hay endpoints de escritura de tareas en este bloque: POST/PUT/PATCH/DELETE se rechazan sin modificar registros. Las futuras acciones deben volver a comprobar propiedad por IdEmpleado al escribir; la ausencia de botones no es una autorización. No se prometen reglas de transición/registro todavía no confirmadas.

### Verificaciones ejecutadas

- **72 pruebas correctas, 0 fallidas y 0 omitidas**: 61 previas y 11 nuevas en TareasEmpleadoPruebas.cs. Se ejecutaron en TestServer con SQL Server LocalDB y bases nuevas BanosQuimicosAuthPruebas_<GUID>, conservadas. No se usó db77593 ni se modificaron cuentas operativas existentes.
- Dos empleados distintos: cada uno lista solo sus propias tareas; IdEmpleado en query no permite elegir al otro; tareas sin asignar quedan fuera; detalle ajeno/inexistente 404. PUT/PATCH/POST/DELETE sobre la tarea ajena 405 y su estado/observaciones permanecen intactos.
- Anónimo 401; Cliente, Administrador y Pendiente 403; Suspendida/BajaLogica con token anterior 401. Empleado cambiado a Cliente pierde acceso con el mismo token. Empleado sin tareas recibe lista vacía y contador 0.
- Reloj fijo solo en el host de pruebas: 5/4/2030 UTC todavía 4/4 en Argentina. Se comprueban ambos límites de medianoche, fecha nula, otra fecha y otro empleado. Incluye una tarea Cancelada del mismo día para comprobar que el contador no representa pendientes.
- Detalle conserva dirección histórica de la tarea aunque el servicio tenga otra ubicación actual. Mantenimiento sin servicio/cliente/ubicación/programación devuelve nulos seguros. Respuestas sin hash ni correo del cliente.
- Razor reales enlazados al proyecto de pruebas existente: contador 1/0, listado, detalle propio, acciones deshabilitadas, Volver al listado y mensaje vacío. Se ejecutan callbacks en el dispatcher con HtmlRenderer; no equivalen a clics ni teclado nativo. No se agregaron paquetes.
- Primer intento de pruebas falló al copiar binarios bloqueados por una API abierta. No se detuvo ese proceso: se compiló/ejecutó con --output en tmp/pruebas-tareas, fuera de Git. Un intento de omitir apphost no era compatible con xUnit v3 y se abandonó, sin cambiar proyectos para esa opción. En la primera suite, 69 pasaron y 3 fallaron por FechaDesde omitida en ubicaciones actuales de los datos de prueba; se completaron las fechas/intervalos sin cambiar restricciones y pasó la suite final.
- App Debug Android y Windows compilada con **0 errores/advertencias**, usando los parámetros temporales de Java ya documentados (heap 512M/SerialGC). API y pruebas compilaron en la salida aislada. La API abierta debe detenerse/recompilarse/reiniciarse antes de probar manualmente las nuevas rutas; no se afirma que su proceso viejo ya las esté atendiendo.
- EF migrations has-pending-model-changes indicó que no hay cambios desde la migración inicial; git confirma sin cambios en entidades/configuraciones/migraciones. Se conservan las 12 tablas de negocio y el historial técnico EF. No se crearon bases o migraciones en hosting.

### Preparación de datos y prueba manual

Se dejó scripts/preparar-tarea-empleado-local.sql, con comprobación de base BanosQuimicosDb e IsLocalDB antes de escribir. Exige empleado y cliente locales de prueba ya registrados/activados por la app; el primero con rol Empleado. No crea cuentas, contraseñas ni cambia roles. Prepara baño PRUEBA-TAREAS-01, servicio entregado, ubicación ficticia marcada y tarea Limpieza extra/Pendiente para hoy a las 10:00 UTC−3. Reutiliza su preparación al repetir el mismo día y no sobrescribe datos ajenos con el código. **Revisado estáticamente, no ejecutado sobre la base principal.** El usuario puede prepararlo según docs/tareas-asignadas-empleado.md, que contiene recorrido, resultados y comandos locales/USB.

Pendiente del estudiante: reiniciar API local con esta versión, preparar esa tarea, recorrer Inicio/listado/detalle/Volver en Windows y Android, comprobar contador, vacío, errores y controles con teclado/desplazamiento/rotación. La API y app usan local/LocalDB; no se publicó ni se contactó MonsterASP. Siguen pendientes vencimiento real del token y alternar servidores en el dispositivo. Registrar Trabajo/Actualizar Estado esperan únicamente la definición y referencia visual indicadas arriba.

### Cierre para revisión

Diff y archivos nuevos revisados; git diff --check correcto. 130 archivos candidatos examinados, índice vacío, sin indicadores de secretos, perfiles de publicación ni generados en los textos/rutas examinados. PDF y PNG originales mantienen hashes SHA-256. Se conserva UsarHosting=false y la conexión USB/certificados existentes; sin cambios de firewall. No se hicieron commits, push ni publicación.

Bloque de **consulta de tareas asignadas listo para revisión**; las acciones de escritura quedan pendientes de definición, sin inventar su flujo. Mensaje sugerido: `feat: consultar tareas asignadas y detalle del empleado`. Esperar confirmación antes de otro bloque.

### Cierre de consulta: prueba manual Windows aprobada (06/11/2026)

El estudiante confirmó personalmente el recorrido **Inicio → Tareas asignadas → Detalle → Volver al listado → Volver al inicio** en Windows, con API local y LocalDB. Todo funcionó y los datos coincidieron con la tarea preparada. **Prueba manual aprobada**, separada de las 72 pruebas automatizadas y compilaciones ya registradas; no se repitieron estas últimas en esta actualización documental.

Actualización de la preparación anterior: por autorización del estudiante se ejecutó el script exclusivamente en LocalDB / BanosQuimicosDb. Se comprobó IsLocalDB=1. Quedó IdTarea=1, asignada a IdEmpleado=1004 (PRUEBA CUENTAS Windows, Empleado/Activa), baño PRUEBA-TAREAS-01, Limpieza extra/Pendiente, cliente Cliente Prueba, dirección ficticia PRUEBA LOCAL - Av. de ejemplo 123 y programación 06/11/2026 a las 10:00 UTC−3. No se modificaron cuentas ni contraseñas. El primer intento revirtió la transacción porque sqlcmd requería QUOTED_IDENTIFIER; se repitió con -I y se verificó la asignación mediante SELECT. Esta evidencia reemplaza la indicación anterior de script aún no ejecutado.

**Bloque de consulta cerrado para commit por indicación del estudiante.** Permanecen pendientes:
- Prueba manual del recorrido en Android.
- Registrar Trabajo y Actualizar Estado: definición del reparto de datos/confirmación y referencia aprobada si requieren otra pantalla; continúan deshabilitados.
- Vencimiento real del token y alternancia de servidores en el dispositivo, pendientes históricos.

La aprobación corresponde al recorrido y datos confirmados en Windows; no se atribuyen pruebas manuales adicionales de desconexión, vacío o permisos a esta confirmación. Sin publicación, commit, push ni inicio de otro bloque. Mensaje sugerido: feat: consultar tareas asignadas y detalle del empleado.

## 18. Registrar Trabajo y Actualizar Estado: cumplimiento local

### Alcance y adaptación autorizados (06/11/2026)

El estudiante confirmó integración/pull del bloque de consulta. Se comprobó master limpio en 26ee141 (merge PR #7) y se creó feature/registrar-trabajo desde ese punto, conservando la versión existente. Se revisaron AGENTS.md, el plan, texto del PDF aprobado (páginas 1 y 2), referencia visual de página 5, captura ampliada adjunta, modelo y código. No se modificaron los documentos originales. No se hizo pull/fetch adicional ni se publicó.

La nueva definición resuelve el pendiente anterior: **todo ocurre en el detalle de tarea existente**, sin formularios, desplegables ni pantallas adicionales. Registrar Trabajo persiste autor y fecha/hora del cumplimiento declarado sin cambiar el estado; Actualizar Estado, habilitado después, finaliza con el estado interno Realizada y muestra Realizado en verde. Registro/estado se recuperan desde la API al volver o reiniciar sesión. No se cargan observaciones ni valores de papel higiénico ficticios. El alcance no incluye captura de esos dos campos: solo se muestran si ya existen.

### Ajuste del modelo explicado antes de aplicarlo

FechaRegistro correspondía a creación de la tarea y FechaRealizada a su cierre; ninguna identificaba el primer paso de manera independiente. Se explicó la necesidad de dos columnas opcionales en TAREAS_OPERATIVAS: FechaTrabajoRegistrado e IdEmpleadoRegistroTrabajo. Migración **20261106204848_RegistroTrabajoTarea**, con FK NoAction, índice y CHECK de autor/fecha juntos. Se conservan las 12 tablas; tareas históricas sin esos datos siguen nulas. No se utiliza FechaInicioReal, observaciones ni papel como sustitutos del registro. Migración generada por EF y revisada antes de aplicar.

Se verificó la conexión explícita a (localdb)\MSSQLLocalDB/BanosQuimicosDb e IsLocalDB=1. Se aplicó allí y se comprobaron ambas migraciones en el historial, 12 tablas de negocio, 27 filas de catálogos y conservación de 6 usuarios y 1 tarea. La tarea de prueba 1/PRUEBA-TAREAS-01/IdEmpleado=1004 sigue Pendiente y sin registro para la prueba personal. No se usaron credenciales del estudiante ni se ejecutó cumplimiento sobre su tarea. MonsterASP/db77593 no se contactó ni modificó. Una futura publicación requerirá aplicar esta nueva migración antes de usar la API nueva; el script anterior 0→Inicial conserva su alcance histórico y no incluye este cambio.

### Implementación y reglas

- TareasController y contratos: POST /api/tareas/{id}/registrar-trabajo y /finalizar, sin cuerpo, devuelven detalle persistido. Política Empleado/Activa y verificación de cuenta dentro de la transacción. Propiedad por identidad autenticada, sin aceptar autor/fecha desde la app. Ajena/inexistente 404; cancelada o contexto incompatible 409.
- Transacción Serializable, bloqueo de aplicación de SQL por base y UPDLOCK/HOLDLOCK de tarea ordenan estas escrituras, incluidas distintas tareas de una solicitud. Registro repetido conserva fecha/autor; cierre repetido no duplica efectos. Pendiente/En curso conservan su estado en el primer paso. Una Realizada histórica no se reescribe para simular registro. Reasignación sin registro permite al nuevo dueño operar; con registro de otro empleado exige revisión administrativa, conservando autor original. No hay endpoint administrativo de reasignación en este bloque.
- Cierre atómico: entrega confirma servicio; retiro lo finaliza y libera asignación; traslado cambia vigencia de ubicaciones preparadas y fecha efectiva, conservando referencias históricas. Validaciones de baño/servicio/ubicaciones y solicitud/cliente. Limpieza/mantenimiento no cambian condición física a Apto ni rellenan papel/observaciones. Realizada elimina el bloqueo de mantenimiento derivado de estados Pendiente/En curso.
- Resolución de solicitud directamente vinculada: requiere Aprobada y todas sus tareas Realizadas, sin excluir Canceladas. Nuevo servicio/baño adicional comprueban cantidad de servicios y entregas cumplidas; baja, servicio finalizado; traslado, fecha efectiva. No resolver una solicitud sin tareas, rechazada o cancelada. Un caso con tarea cancelada se conserva para revisión administrativa: el modelo no identifica por sí solo qué trabajo la reemplaza, y no se inventa esa equivalencia.
- DTO de consulta ampliado con origen/descripción de solicitud (o la de origen del servicio si falta vínculo directo), observaciones asociadas, reposición ya guardada, autor y fechas, y permisos de los dos botones. Sin hashes ni tokens. No se agrega un tipo de tarea Reposición al catálogo: las indicaciones disponibles pueden venir de la descripción del reclamo/solicitud.
- ClienteAutenticacion, SesionUsuario y TareasAsignadas.razor: peticiones autenticadas al destino existente, sin envíos duplicados mientras está ocupado, confirmación solo con respuesta guardada, lectura al reabrir, errores/reintento y botones según datos del servidor. El estado Realizada se presenta como Realizado en verde. Se conserva navegación y manejo de sesión; el error retira el detalle anterior hasta volver a consultarlo. CSS solo amplía estado verde y texto largo. Sin cambios en manifiestos, certificados o selección del servidor (UsarHosting=false).

### Pruebas y compilación realizadas

- **85 pruebas automatizadas correctas, 0 fallidas, 0 omitidas**, frente a 72 anteriores. Primera ejecución de esta ronda: 82 correctas; se agregaron tres escenarios y se repitió la suite final de 85. TestServer y SQL Server LocalDB en bases nuevas BanosQuimicosAuthPruebas_<GUID>, conservadas, sin borrar ni alterar cuentas existentes.
- Dos pasos y nuevo login con las mismas credenciales: primer paso deja estado/FechaRealizada intactos y guarda autor/fecha; nuevo cliente HTTP recupera el registro y finaliza. Repeticiones conservan autor/fechas; no generan registros adicionales ni rellenan observaciones/papel. Se conserva condición física.
- Cuatro registros y cuatro cierres simultáneos de la misma tarea; dos tareas de una solicitud con cierre simultáneo; estados correctos y resolución única. Entrega/retiro y traslado conservan efectos al repetir; histórico de ubicaciones sin redirección. Solicitud de varios baños no se resuelve por una sola entrega; reclamo muestra descripción y no se resuelve mientras quede trabajo cancelado/pendiente.
- Otro empleado 404; reasignación después del registro impide al anterior operar y al nuevo apropiarse del registro. Cancelada e histórica Realizada rechazan escrituras; En curso conserva estado al registrar, y cancelar entre pasos impide cierre. Traslado incompleto falla sin efectos parciales. Las pruebas de permisos existentes se ampliaron a ambos POST: anónimo 401, Cliente/Administrador/Pendiente 403, suspendida/baja 401.
- Razor reales con HtmlRenderer: registro visible, recuperación al abrir detalle, cierre con Realizado y clase verde, regreso al listado y vacío. Son pruebas de componentes/callbacks, no interacción nativa, SecureStorage real o reinicio del teléfono.
- API/pruebas compiladas correctamente en tmp/pruebas-trabajo; app Debug Windows y Android compiladas con **0 errores y 0 advertencias**, con parámetros temporales JavaMaximumHeapSize=512M / JavaOptions=-XX:+UseSerialGC ya documentados. No se cambiaron herramientas ni paquetes.
- La API abierta en Visual Studio (proceso 11272) bloqueó apphost de Debug y frustró la generación inicial. Se conservó ese proceso y se usó configuración de compilación TrabajoLocal para generar/aplicar migración y salida tmp para tests. No se detuvo la API del estudiante ni se modificó el csproj para esa configuración. Es necesario detenerla, recompilar y reiniciar para probar los endpoints nuevos.

### Aclaración semanal para el futuro bloque administrativo

El acuerdo previo era programación manual de cada tarea, con frecuencia del servicio solo referencial. El estudiante aclaró ahora su intención: **asignar baños a empleados y que esas asignaciones generen las tareas semanales**. Se registra como cambio de intención para el futuro bloque administrativo, separado de lo ya implementado; no se genera ninguna tarea automáticamente aquí. Allí se concretarán días/fechas, repetición sin duplicados, cambios/final de asignación y su representación en el modelo antes de implementarlo. Este bloque solo registra y finaliza tareas existentes.

### Revisión y pendientes

Guía reproducible: docs/registrar-trabajo.md. Pruebas manuales de los dos pasos en Windows y Android pendientes: registrar, recuperar entre sesiones, finalizar, reabrir y comprobar verde/fechas. La aprobación Windows del bloque de consulta permanece válida para aquel alcance; no se extiende a estos controles. Continúan pendientes vencimiento real del token y alternancia de servidores en el dispositivo.

Bloque implementado y listo para revisión; no se inicia otro bloque. Sin commit, push ni publicación. Mensaje sugerido: **feat: registrar cumplimiento y finalizar tareas del empleado**.

Revisión final: EF indicó que no hay cambios del modelo pendientes de migrar; git diff --check correcto. 133 archivos versionados/candidatos examinados: sin rutas de generados, perfiles de publicación ni indicadores de credenciales/tokens/claves privadas en los textos revisados; índice de Git vacío. PDF y dos PNG originales mantienen sus SHA-256 anteriores. No se volvieron a ejecutar tests tras los cambios finales exclusivamente documentales.

### Cierre para commit: prueba manual Windows aprobada (06/11/2026)

El estudiante confirmó que probó el recorrido indicado en docs/registrar-trabajo.md en Windows y funcionó correctamente: registro del trabajo, recuperación del registro al volver o iniciar sesión, actualización del estado y persistencia del cierre. **Prueba manual Windows aprobada**, independiente de las 85 pruebas automatizadas y las compilaciones ya registradas. No se repitieron tests ni compilaciones para esta actualización documental. La condición Pendiente/sin registro de la tarea de prueba descrita arriba corresponde a su preparación anterior a esta prueba; no se restablece ni se modifica su resultado.

Detalle de la migración aplicada **20261106204848_RegistroTrabajoTarea** en LocalDB/BanosQuimicosDb:

- Agregó FechaTrabajoRegistrado, tipo datetimeoffset, opcional, e IdEmpleadoRegistroTrabajo, tipo int, opcional, a TAREAS_OPERATIVAS.
- Creó IX_TAREAS_OPERATIVAS_IdEmpleadoRegistroTrabajo y la FK a USUARIOS.IdUsuario sin borrado en cascada.
- Agregó CK_TAREAS_OPERATIVAS_RegistroTrabajo: autor y fecha deben ser ambos nulos o ambos tener valor.
- Registró la nueva migración en __EFMigrationsHistory. Conservó las 12 tablas de negocio, catálogos y datos; no creó usuarios ni completó retrospectivamente registros de trabajo. FechaRealizada sigue representando el segundo paso, sin reutilizar observaciones ni papel higiénico.

**Pendiente para la futura publicación:** revisar el estado y las migraciones de db77593 y aplicar esta migración aditiva en MonsterASP antes de utilizar allí la API de este bloque. Todavía no se aplicó al hosting ni se publicó la API. El script 0→Inicial no incluye este cambio y no debe repetirse sobre la base alojada existente; el despliegue deberá llevarla desde su migración efectivamente aplicada hasta RegistroTrabajoTarea, conservando los datos. Esta anotación no autoriza ejecutar la publicación.

**Bloque cerrado y listo para commit**, conservando como pendientes la prueba manual Android, la migración/publicación en hosting, el vencimiento real del token y la alternancia de servidores en el dispositivo. No se hizo commit ni push ni se inició otro bloque. Mensaje sugerido: **feat: registrar cumplimiento y finalizar tareas del empleado**.

## 19. Administrador: Estado Baños, responsable habitual y limpieza manual

### Inicio y decisiones confirmadas (06/11/2026)

El estudiante informó integración/pull del bloque anterior. Se verificó master limpio en 898e3f5 (merge PR #8) y se creó feature/estado-banos desde ese punto. No se hizo fetch/pull adicional ni se descartaron cambios. Se leyeron AGENTS.md, el plan, texto funcional del PDF, página 6 renderizada, DER original, capturas ampliadas aportadas y código actual. Los originales se conservan.

Decisiones confirmadas que prevalecen sobre las propuestas anteriores:

- Cada baño tendrá responsable habitual de limpieza. Solicitudes/reclamos admiten un empleado puntual distinto, sin cambiar al habitual.
- **Programación manual por fecha de cada tarea**; frecuencia del servicio solo referencial. Esto reemplaza la intención de generación semanal automática registrada en la sección 18. No se agrega ningún generador.
- Al programar se propone el habitual, pudiendo elegir otro para ese trabajo. Asignar ▾ abre selección de empleados dentro de la tarjeta.
- El cliente completa los datos de servicio nuevo; administración lo recibe en Solicitudes y Reclamos y selecciona baños disponibles. Ese recorrido queda documentado para el bloque posterior.
- Autorización visual explícita del estudiante: selector de fecha y futura pantalla para resolver servicio nuevo basada en Resolver solicitudes y reclamos, con selección de baños. Es una ampliación concreta de las autorizaciones visuales anteriores; no permite inventar otras pantallas. Aquí solo se usa la primera, en Estado Baños.

### Verificación del modelo y migración explicada previamente

IdEmpleado/FechaProgramada de TAREAS_OPERATIVAS ya representan empleado puntual y planificación, pero ni el DER ni BanoQuimico tenían un responsable habitual independiente. Se explicó el mínimo ajuste antes de editar o aplicar: **20261106222844_ResponsableLimpiezaYProgramacion**, agregando BANOS_QUIMICOS.IdResponsableLimpieza opcional (FK NoAction a USUARIOS e índice) y TAREAS_OPERATIVAS.ClaveProgramacion opcional (uniqueidentifier e índice único filtrado para reintentos). Se mantienen 12 tablas; no se reutilizan observaciones ni el empleado de una tarea pasada para inventar responsables. Las unidades existentes quedan Sin asignar hasta intervención administrativa.

Migración generada con EF, revisada y aplicada solo en (localdb)\MSSQLLocalDB/BanosQuimicosDb, después de comprobar nombre e IsLocalDB=1. Verificación posterior: 12 tablas de negocio, historial con Inicial/RegistroTrabajoTarea/ResponsableLimpiezaYProgramacion, 6 usuarios, 1 baño y 1 tarea conservados. PRUEBA-TAREAS-01 permanece con responsable null y la tarea 1 Realizada con clave null. No se crearon programaciones ni se alteraron asignaciones operativas existentes para las pruebas automatizadas.

Pendiente para publicación: revisar el historial real de db77593 y aplicar las migraciones faltantes de RegistroTrabajoTarea y ResponsableLimpiezaYProgramacion antes de utilizar esta API. No se ejecutó nada ni se contactó MonsterASP. El script inicial histórico no se modifica ni se repite sobre bases existentes.

### Implementación y comprobaciones de negocio

- BanosController y Contratos/Banos: GET /api/banos/estado y /empleados-asignables, PUT /api/banos/{id}/responsable-limpieza, POST /api/banos/{id}/limpiezas. Solo Administrador/Activa; empleados elegibles Empleado/Activa. Se revalidan cuentas en la transacción. DTOs de lectura limitados a contexto y nombres/IDs necesarios, sin hashes/contraseñas/tokens.
- Disponibilidad derivada de Apto + sin servicio asignado (Pendiente de entrega/Entregado) + sin mantenimiento bloqueante Pendiente/En curso. Condición, servicio y mantenimiento se muestran separados. Asignar responsable no es asignar un servicio y admite unidades disponibles, asignadas o No apto. No cambia condición ni historial.
- Programar limpieza semanal crea Tarea/Pendiente y fija servicio entregado del mismo baño, ubicación actual, empleado elegido y fecha. No crea servicios para unidades libres ni programa contra pendientes de entrega o ubicaciones obsoletas. No apto/bloqueos restringen nuevas asignaciones de servicio, no la limpieza de uno ya entregado; se advierte en la tarjeta sin liberar/asignar el baño ni cambiar su condición. Limpieza pendiente no bloquea por sí sola. El empleado puede registrar y finalizar con los endpoints anteriores.
- Fecha elegida se guarda como día argentino a las 00:00 UTC−3, separada de fechas de registro/realización. No se inventa hora efectiva, observaciones o papel. La frecuencia no limita ni genera tareas. Se conserva el empleado de cada tarea aunque cambie el habitual.
- Repetición con la misma ClaveProgramacion/datos devuelve el mismo IdTarea; primera creación 201, reintento 200, clave con otros datos 409. También se rechaza una programación nueva idéntica en baño/servicio/ubicación/empleado/fecha mientras la anterior no esté cancelada, para evitar duplicados al volver a entrar. No se limita a una limpieza por semana ni se prohíbe elegir otro empleado o fecha. ResponsableAnterior evita pisar cambios concurrentes de responsable (no detecta una ida/vuelta completa al mismo valor). Transacciones Serializable con el mismo bloqueo de aplicación de cumplimiento ordenan operaciones; índice único refuerza la clave.
- App: nuevos ContratosBanos y Components/Administracion/EstadoBanos.razor; ampliación de ClienteAutenticacion/SesionUsuario/Home/CSS. Tarjetas según referencia, Asignar ▾, selector de empleados activos, guardado habitual, fecha requerida y programación, confirmación solo tras respuesta persistida, carga/vacío/error/reintento/regreso. Propone el habitual activo al abrir. Los datos del reintento se mantienen inmutables mientras su resultado no se confirma; no se envían duplicados durante procesamiento. Baños y Servicios/Solicitudes y Reclamos siguen sin implementar y deshabilitados en el inicio. Conservados local/USB/certificados/almacenamiento de sesión.

### Pruebas y compilación

- **100 pruebas automatizadas correctas, 0 fallidas, 0 omitidas**: 85 anteriores y 15 casos nuevos. TestServer, cliente C# real y LocalDB en bases nuevas BanosQuimicosAuthPruebas_<GUID> conservadas. Ninguna usa la base alojada o las cuentas operativas locales.
- Recorrido administrador → asignación habitual A → limpieza puntual B → lista/contador de B → registro/cierre por B; A recibe 404 al consultar el detalle ajeno. Se conserva habitual A y empleado de la tarea anterior, ubicación actual al programar y fecha UTC−3. No se completan papel/observaciones ficticios.
- Cuatro envíos concurrentes con una clave generan una tarea y devuelven el mismo ID; clave alterada y nueva clave/datos idénticos no duplican. Cambios concurrentes de habitual: uno se guarda y otro 409; repetición del guardado inocua. Cliente/Empleado/Pendiente 403, anónimo y suspendido 401. Selección rechaza Cliente, Pendiente, Suspendida y BajaLogica; lista solo empleados activos.
- Disponible con mantenimiento no bloqueante, bloqueo real que impide disponibilidad, No apto ya entregado que conserva servicio/condición y puede programar limpieza; unidad sin servicio y pendiente de entrega rechazadas para esta operación. Ubicación histórica/servicio desactualizado, datos extra/incompletos y baño inexistente rechazados sin crear tareas.
- Renderer de Razor real: tarjeta, apertura de Asignar, confirmación visible de responsable, propuesta del habitual, programación y pulsación repetida sin crear otra tarea. Se enlazó el nuevo componente en el proyecto de pruebas; se reutilizaron helpers existentes de datos. No equivale a eventos nativos ni prueba manual Windows/Android.
- Intento inicial de compilación de tests falló por alias App ambiguo; se corrigió a ClienteApp y se resolvieron advertencias del analizador. Primera suite: 97 correctas/3 fallidas. La escritura de habitual funcionaba, pero el filtro aplicado sobre DTO record no era traducible por EF y fallaba su respuesta. Se reprodujo aisladamente, movió el filtro por ID antes de la proyección y reforzó el test Razor para exigir la confirmación. Suite corregida: 100 correctas. Tras finalizar validación de fecha y lectura autenticada compartida, se repitió la suite: **100 correctas**.
- API/pruebas compilaron en salida tmp/pruebas-estado-banos; app Debug Windows y Android con **0 errores y 0 advertencias**, usando los parámetros temporales JavaMaximumHeapSize=512M/JavaOptions=-XX:+UseSerialGC ya documentados, sin modificar configuración global ni paquetes. EF usó configuración de compilación TrabajoLocal para evitar tocar los binarios de una API abierta. No se detuvo el proceso del estudiante: debe reiniciarlo con la versión actual para su prueba.

### Guía, próximo alcance y cierre

docs/estado-banos.md contiene prueba manual con la unidad local PRUEBA-TAREAS-01, pasos administrador → empleado, contratos, migración y recorrido de servicio nuevo para el bloque siguiente. La futura resolución seleccionará baños realmente disponibles, creará un servicio/ubicación por unidad y trabajos puntuales, conservará solicitud origen y distinguirá aprobación de resolución. La pantalla autorizada de selección de baños no se implementó en este bloque.

Pendientes: prueba manual de este bloque en Windows y Android; migraciones/publicación futura en hosting; vencimiento real del token y alternar servidores en el dispositivo. Las aprobaciones Windows de consulta/cumplimiento previas se conservan para esos alcances, sin extenderlas a la programación administrativa nueva.

Bloque implementado y listo para revisión, sin commit, push ni publicación ni comienzo de otro bloque. Mensaje sugerido: **feat: asignar responsables y programar limpiezas desde Estado Baños**.

Revisión final: EF confirmó modelo sin cambios pendientes de migrar; git diff --check correcto. 141 archivos versionados/candidatos examinados, sin rutas de generados/perfiles privados ni indicadores de credenciales, tokens o claves privadas en los textos revisados; índice vacío. PDF y PNG originales mantienen los SHA-256 anteriores. Las últimas modificaciones fueron documentales; no se repitieron tests ni compilaciones después de ellas.

### Cierre: prueba manual Windows aprobada

El estudiante confirmó personalmente el recorrido completo en Windows con API local y LocalDB: guardar responsable habitual → programar limpieza para el **07/11/2026** → ingresar como empleado → consultar la tarea → Registrar Trabajo → Actualizar Estado/finalizar. **Prueba manual aprobada.** La fecha indicada es la programación de la limpieza, no una fecha de ejecución inferida. No se atribuye a esta confirmación la prueba manual de otros empleados, concurrencia o errores; esas evidencias automatizadas permanecen separadas.

Se conserva el resultado y el historial de la prueba; no se restablecen tareas ni se modifican datos en este cierre documental. Las 100 pruebas automatizadas y las compilaciones previas no se repitieron porque no cambió código. No se documentaron credenciales ni tokens.

Migración local a incluir en la futura actualización del hosting: **20261106222844_ResponsableLimpiezaYProgramacion**. Agregó IdResponsableLimpieza (int opcional) a BANOS_QUIMICOS, con índice y FK a USUARIOS sin cascada; agregó ClaveProgramacion (uniqueidentifier opcional) a TAREAS_OPERATIVAS, con índice único filtrado para valores no nulos. Su aplicación se registró en __EFMigrationsHistory, conservando las 12 tablas, catálogos y datos existentes. Los registros anteriores quedaron sin valores inventados.

**Hosting pendiente:** antes de publicar, consultar el historial real de db77593 y aplicar las migraciones faltantes en orden, incluyendo RegistroTrabajoTarea y ResponsableLimpiezaYProgramacion según corresponda. No repetir el script inicial sobre la base existente ni copiar usuarios o datos locales. Esta actualización no aplicó migraciones en MonsterASP ni autoriza su publicación.

**Bloque cerrado y listo para commit**, por indicación del estudiante. Continúan pendientes Android, migraciones/publicación en hosting, vencimiento real del token y alternancia de servidores en el dispositivo. Sin commit, push ni inicio de otro bloque. Mensaje sugerido: **feat: asignar responsables y programar limpiezas desde Estado Baños**.

## 20. Administrador: Solicitudes y Reclamos

### Punto de partida y decisiones confirmadas

Master estaba limpio en 7ab2718 (merge PR #9), actualizado por el estudiante. Se creó feature/solicitudes-reclamos sin descartar cambios. Se revisaron AGENTS.md, el plan, modelo/código, texto funcional del PDF y referencia visual de página 6. Se conservaron y completaron los contratos y consultas existentes antes de incorporar la interfaz, la asignación y sus verificaciones.

El estudiante confirmó expresamente dos decisiones que faltaban:

- En un reclamo con trabajo, el administrador elige Limpieza extra o Mantenimiento en el mismo detalle; para mantenimiento indica si bloquea nuevas asignaciones. Se autorizan esos controles, sin otra pantalla.
- **Aprobar y asignar** es una sola confirmación y transacción: solicitud Aprobada y tarea Pendiente. Solo completar los trabajos requeridos resuelve la solicitud.

Se mantiene cualquier empleado activo como elección puntual, independiente del habitual. La programación es manual con fecha elegida por administración, reutilizando el selector autorizado; la fecha deseada del cliente es solo referencia. No se agregan pagos/facturación ni una pantalla fuera de las referencias/autorizaciones expresas.

### Modelo, implementación y límites

No se necesitó modificar entidades, configuraciones EF o migraciones. Se reutilizan las 12 tablas, TAREAS_OPERATIVAS.IdSolicitud/IdEmpleado/FechaProgramada/ClaveProgramacion y las ubicaciones de origen/destino existentes.

- API/Controllers/SolicitudesController.cs y Contratos/Solicitudes: GET /api/solicitudes, GET /api/solicitudes/{id}, POST /api/solicitudes/{id}/asignar. Solo Administrador/Activa; DTOs de identidad/contexto/trabajos sin credenciales ni hashes. Listado estable por FechaSolicitud/IdSolicitud descendentes, detalle con datos presentes y trabajos vinculados. Sin baño ficticio para servicio nuevo.
- Asignación valida dentro de transacción Serializable al administrador, empleado Empleado/Activa, estado anterior, servicio Entregado del mismo cliente y ubicación actual esperada. Se comparte el bloqueo SQL de programación/cumplimiento. Una clave repetida con mismos datos devuelve el trabajo persistido; clave con otros datos/cancelación produce 409. Otra clave no genera un nuevo trabajo si la solicitud ya tiene historial. No se reasignan ni sustituyen trabajos anteriores, incluidos los cancelados, desde este botón. Estados terminales no se reabren.
- Limpieza extra → Limpieza extra; Baja → Retiro; cambio Solicitado → Traslado; reclamo → Limpieza extra/Mantenimiento por elección explícita. Solo mantenimiento acepta bloqueo. Se fija el contexto de tarea sin cambiar al responsable habitual ni rellenar papel/observaciones. Traslado prepara destino no vigente: el cierre existente hace efectivo el cambio. No apto/bloqueos no liberan un servicio existente ni se convierten en Apto al atenderlo.
- Servicio nuevo/baño adicional quedan en consulta: requieren selección de unidades y servicios en su bloque, no se aprueban ni procesan como limpieza. Cambio Informado requiere validación administrativa, no otro traslado. Reclamos sin trabajos requieren respuesta administrativa, sin tarea ficticia; la captura de esa respuesta no se implementa aquí. No se crea endpoint de rechazo/cancelación/borrado. La papelera no tiene regla aprobada y se omite; no se interpreta su dibujo como permiso para borrar.
- El cierre de tareas existente resuelve solo solicitudes aprobadas con todos los trabajos requeridos realizados; canceladas no se ignoran. Retiro cierra servicio, traslado conserva historia, mantenimiento no cambia automáticamente condición física. No se sustituye aprobación por resolución ni se resuelve por tener cero tareas.
- App/Autenticacion/ContratosSolicitudes.cs, ClienteAutenticacion/SesionUsuario, Components/Administracion/SolicitudesReclamos.razor, Home y CSS: acceso habilitado; listado/detalle según referencia, carga/vacío/error/reintento/Volver; Asignar ▾ abre controles dentro del detalle, selección sin guardar, Cancelar y confirmación explícita. Tras guardar muestra solicitud/tarea actuales y, al consultar otra vez después del cierre, la resolución. Conservadas reglas de sesión, servidor local, USB y validación HTTPS.

### Datos locales preparados y conservados

Se revisó y ejecutó scripts/preparar-solicitudes-local.sql, con guardas de nombre BanosQuimicosDb e IsLocalDB=1. Reutiliza el servicio Entregado de PRUEBA-TAREAS-01 y cliente de prueba existentes; no crea cuentas ni modifica contraseñas, responsable o tareas anteriores. Insertó solo tres solicitudes identificadas por PRUEBA LOCAL SOLICITUDES:

- IdSolicitud 1: Limpieza extra.
- IdSolicitud 2: Reclamo con descripción de revisión.
- IdSolicitud 3: Nuevo servicio, ejemplo de consulta sin baño/servicio seleccionado.

Las tres quedaron Pendientes con cero trabajos para la prueba personal. Una segunda ejecución insertó cero solicitudes, conservando estados. Verificación: 12 tablas de negocio, 6 usuarios, 2 tareas anteriores conservadas; responsable de PRUEBA-TAREAS-01 sigue IdUsuario 1004. Baja/traslado se prueban solo en bases aisladas para no retirar o trasladar el servicio local utilizado en otras pruebas. No se contactó ni modificó MonsterASP/db77593.

### Verificaciones automatizadas y compilación

- Primera suite: **121 correctas**. Se agregaron cuatro escenarios y se verificó el renderer vacío: suite final **125 correctas, 0 fallidas, 0 omitidas** (100 anteriores y 25 casos nuevos). TestServer, cliente C# y LocalDB en bases nuevas BanosQuimicosAuthPruebas_<GUID> conservadas. Datos de prueba claramente identificados y credenciales aleatorias internas, no impresas ni documentadas.
- Limpieza extra, ambos tipos de reclamo, baja y traslado solicitado: asignación aprueba sin resolver, crea trabajo Pendiente y conserva habitual; empleado distinto del habitual puede consultar/registrar/finalizar, otro empleado recibe 404. El cierre y su repetición dejan solicitud Resuelta y efectos de servicio/ubicación correctos, sin papel/observaciones inventados.
- Cuatro reintentos concurrentes con una clave generan una tarea y un único destino de traslado; clave alterada o nueva sobre historial no duplica. Dos asignaciones distintas concurrentes: una correcta y otra 409, sin pisar empleado/estado. Trabajos cancelados mantienen solicitud Aprobada, sin resolución ni reasignación silenciosa.
- Nuevo/adicional e Informado no se aprueban ni generan tareas desde asignación. Rechazada/Resuelta/Cancelada no se reabren. Sin destino, contexto de otro cliente o ubicación incorrecta, empleado suspendido, bloqueo ajeno a mantenimiento, campos extra y solicitud inexistente: rechazo sin efectos parciales. DELETE por ID responde 405 y conserva solicitud.
- Anónimo 401; Cliente/Empleado/Pendiente 403; Suspendida/BajaLogica con token anterior 401 para consultas y asignación. Listado/detalle seguros, orden estable y lista vacía. Componentes Razor reales: lista/detalle, selección sin escritura, cancelar, confirmación visible, solicitud Aprobada, cierre del empleado y consulta actualizada Resuelta; también vacío. No son eventos nativos ni prueba manual en dispositivos.
- API/pruebas compilaron en tmp/pruebas-solicitudes y app Debug Windows/Android compiló con **0 errores y 0 advertencias**, usando parámetros temporales JavaMaximumHeapSize=512M / JavaOptions=-XX:+UseSerialGC ya documentados. Sin instalar herramientas/paquetes, sin cambios globales ni migración nueva. El estudiante debe reiniciar la API local con esta versión antes de probar las rutas nuevas.

### Guía y decisión de validación/publicación

Guía Windows en docs/solicitudes-reclamos.md: administrador → ejemplo 1/2 → elegir empleado/fecha/tipo si corresponde → Aprobar y asignar → empleado registra/finaliza → administrador consulta resolución. Servicio nuevo de ejemplo 3 solo consulta. Los dos recorridos manuales Windows de este bloque fueron aprobados según el cierre siguiente; las aprobaciones no se extienden a controles no confirmados.

**Decisión de trabajo confirmada:** completar los bloques funcionales antes de la prueba integral manual Android, que será contra API local. Solo después se abordarán republicación y migraciones del hosting. Se conserva como pendiente esa validación integral, el vencimiento real del token y alternar servidores en el dispositivo. La compilación Android no los da por aprobados.

Bloque implementado y listo para revisión. Sin commit, push, merge ni publicación. Mensaje sugerido: **feat: asignar solicitudes y reclamos a tareas del empleado**. Esperar confirmación antes de comenzar el siguiente bloque.

Revisión final: EF confirmó que no hay cambios del modelo pendientes; git diff --check correcto. Se revisaron diff/archivos nuevos y 149 candidatos versionados/no ignorados, sin rutas de generados o perfiles privados ni indicadores de credenciales/tokens/claves privadas en los textos examinados; índice vacío. PDF y PNG originales conservan SHA-256. No se repitieron pruebas después de los últimos cambios exclusivamente documentales.

### Cierre: verificaciones manuales Windows aprobadas

Se confirmaron personalmente en Windows, con API local y LocalDB, los dos recorridos:

- **Limpieza extra aprobada:** Aprobar y asignar → empleado registra el trabajo y finaliza → solicitud Resuelta.
- **Reclamo con mantenimiento aprobado:** elegir Mantenimiento e indicador de bloqueo → asignar empleado → comprobar bloqueo del baño → completar trabajo → reclamo Resuelto y bloqueo levantado.

Estas verificaciones manuales se registran separadas de las **125 pruebas automatizadas anteriores** y de las compilaciones Windows/Android ya documentadas. No se repitieron pruebas automatizadas ni compilaciones para este cierre, que solo actualiza documentación. Los resultados y trabajos guardados se conservan como historial; no se restablecen solicitudes o tareas ni se registran credenciales/tokens.

**Bloque cerrado y listo para commit.** La prueba integral manual Android continúa pendiente hasta terminar los bloques funcionales, contra API local y antes de republicar. La actualización/migraciones del hosting se mantienen para después de esa validación. Siguen pendientes vencimiento real del token y alternancia de servidores en el dispositivo. No se modificó el hosting ni se hizo commit, push, merge o se comenzó otro bloque.

Mensaje de commit/título de PR sugerido: **feat: asignar solicitudes y reclamos a tareas del empleado**. La descripción del PR debe resumir listado/detalle, aprobación y asignación atómicas, resolución mediante trabajos, conservación del habitual/historial y protección de permisos/reintentos; distinguir 125 pruebas automatizadas, compilación y los dos recorridos Windows aprobados del pendiente integral Android. Este bloque no agrega migraciones.

## 21. Servicio nuevo: asignación de baños disponibles y entregas

### Revisión y decisiones confirmadas (07/11/2026)

Se comprobó master limpio en 5ac50d4 (merge PR #10), actualizado por el estudiante, y se creó feature/asignar-banos-servicio sin descartar cambios ni hacer pull/fetch adicional. Se revisaron AGENTS.md, texto funcional del PDF, referencia visual Resolver solicitudes y reclamos de página 6, DER, reglas del plan, modelo, índices y recorrido de tareas existente. Se conserva la autorización expresa para adaptar el detalle con selección de baños; no se modifican los originales ni se crean pantallas ajenas.

Se consultaron y confirmaron las dos decisiones faltantes:

- Frecuencia común para los servicios del pedido, indicada por administración; 0 cuando no corresponda. Solo referencia para programación manual.
- Un empleado activo y una fecha programada comunes a todas las entregas del pedido.

Se respeta exactamente la cantidad y el tipo físico solicitado. Tipo de baño y uso Obra/Evento son independientes: seleccionar tipo Obra para un uso Evento sigue permitido si el cliente pidió ese tipo. No se inventa frecuencia desde una solicitud que no contiene ese campo ni se convierte su fecha deseada en fecha programada sin elección administrativa.

### Persistencia e implementación

El modelo existente alcanza: **sin entidades/configuraciones EF/migraciones nuevas**, conservando las 12 tablas e historial. Se reutilizan Servicio.IdSolicitudOrigen/IdCliente/IdBano, ubicación preparada y tarea con IdSolicitud/IdServicio/IdBano/IdUbicacion/IdEmpleado/FechaProgramada/ClaveProgramacion. Se explicó antes de modificar código la confirmación atómica y la distinción entre asignar, entregar y resolver.

- SolicitudesController y nuevo contrato AsignarBanosSolicitud: GET /api/solicitudes/{id}/banos-disponibles y POST /api/solicitudes/{id}/asignar-banos. Exclusivos de Administrador/Activa; revalidación transaccional de administrador/empleado y datos de la solicitud. La selección excluye No apto, tipo físico distinto, asignación vigente y mantenimiento bloqueante Pendiente/En curso. Limpiezas/mantenimientos no bloqueantes no quitan disponibilidad por sí solos.
- Confirmación crea un servicio Pendiente de entrega y una tarea Entrega/Pendiente por unidad, ambos vinculados a solicitud y cliente, con ubicación a la dirección solicitada todavía sin vigencia efectiva. Frecuencia/empleado/fecha comunes se guardan explícitamente. La solicitud queda Aprobada, sin FechaResolucion; el responsable habitual no cambia y la unidad queda asignada desde este momento.
- Cantidad exacta y IDs distintos se validan también en API. Transacción Serializable y mismo bloqueo SQL de los recorridos anteriores; disponibilidad revalidada al guardar e índice único filtrado existente de baño asignado. Si una unidad cambió, se rechaza todo el pedido, sin servicios/ubicaciones/entregas parciales.
- La primera entrega conserva la clave de la operación; las demás reciben claves propias. Un reintento reconoce y vincula el lote guardado; no crea otro. Datos distintos con esa clave o una operación nueva sobre servicios/trabajos previos devuelven 409. No se sobrescriben conjuntos históricos parciales para aparentar una confirmación completa. Baño adicional sigue separado, con su servicio de referencia; este endpoint atiende solo servicio nuevo.
- TareasController: al completar una entrega con ubicación preparada y sin ubicación vigente, la activa con FechaDesde real y registra FechaEntregaReal del servicio. Las ubicaciones históricas y los servicios existentes se conservan. El cierre ya implementado mantiene la solicitud Aprobada hasta completar todas las entregas/cantidades/trabajos necesarios; entonces la resuelve. Registrar Trabajo por sí solo no entrega el baño.
- Detalle de solicitud/contratos agregan servicios vinculados y baño/servicio de cada tarea. App reutiliza SolicitudesReclamos con el componente AsignarBanosServicio, selector de unidades, empleado/fecha/frecuencia comunes, carga/vacío/error/confirmación/cancelación y estados recuperados. Mantiene estilos y navegación. Seleccionar no guarda; solo se informa éxito con respuesta persistida. No se implementa edición, borrado, cancelación, reemplazo o liberación de asignaciones.

### Pruebas y uso de recursos

Se conservaron las **125 pruebas existentes** y se agregaron **9 casos nuevos** en AsignarBanosServicioPruebas.cs, solo para reglas/riesgos del bloque. Sin nuevas pruebas automatizadas para cambios visuales simples; la comprobación de pantalla y navegación se deja a Windows manual.

- Desarrollo: una ejecución focalizada, **9 correctas**, 0 fallidas/omitidas.
- Cierre: **una única suite completa**, **134 correctas**, 0 fallidas/omitidas (125 anteriores + 9 nuevas).
- Total de ejecuciones de casos en esta etapa: **143** (9 focalizadas + 134 completas); casos distintos comprobados: 134. No se repitieron suites ni compilaciones por cambios documentales.
- Nuevos casos: servicios/entregas coherentes por unidad; ubicación preparada que se activa al entregar; primer cumplimiento no resuelve hasta completar el segundo; tipo Obra usado en Evento; conservación de habitual; otro empleado sin acceso a entrega ajena; cantidad/duplicados/tipo/condición/ocupación/bloqueo; dos pedidos simultáneos por un baño con reversión completa del perdedor; reintentos simultáneos sin duplicados; permisos de las rutas nuevas; datos obligatorios/empleado suspendido; historial parcial no sobrescrito. Se usan bases LocalDB aisladas BanosQuimicosAuthPruebas_<GUID>, conservadas.
- API/pruebas compilaron en tmp/pruebas-asignar-banos. App Debug Windows y Android compilaron una vez al cierre con **0 errores y 0 advertencias**, con los parámetros temporales JavaMaximumHeapSize=512M/JavaOptions=-XX:+UseSerialGC ya documentados, sin paquetes/herramientas nuevos ni cambios globales. Hubo una compilación previa del proyecto de pruebas durante el desarrollo, sin ejecutar pruebas entonces.

### Preparación local y guía manual

Se revisó y ejecutó scripts/preparar-servicio-nuevo-local.sql solo en LocalDB/BanosQuimicosDb, con guardas explícitas. Agregó solicitud **4**, **PRUEBA LOCAL SERVICIO NUEVO - DOS BANOS**, cantidad 2/tipo Obra/uso Evento/dirección PRUEBA LOCAL - Entrega de servicio nuevo 789, y unidades **PRUEBA-NUEVO-01** (ID 2) y **PRUEBA-NUEVO-02** (ID 3), Aptas/sin asignación. No se crearon aún sus servicios ni tareas: quedan para la confirmación manual en la app.

Antes: 6 usuarios, 1 baño, 3 solicitudes, 1 servicio y 4 tareas. Después: mismos 6 usuarios/1 servicio/4 tareas, con 3 baños y 4 solicitudes. No se cambiaron cuentas, contraseñas, trabajos ni responsables anteriores. Un intento inicial del script se detuvo por conflicto de collation de su variable de códigos; se corrigió esa variable para coincidir con CodigoInterno, sin alterar la base, y se ejecutó correctamente. Repetir reutiliza datos sin restablecer estados o liberar unidades.

docs/asignar-banos-servicio.md contiene contratos, reglas, datos y guía corta administrador → dos entregas del empleado → solicitud Resuelta. Verificar descripción del pedido 4 para no confundirlo con el ejemplo de servicio nuevo anterior. El estudiante deberá reiniciar la API con esta versión local antes de probar los endpoints nuevos.

El recorrido manual Windows fue aprobado según el cierre siguiente. Pendientes: integral Android al terminar bloques contra API local; después migraciones/republicación del hosting. Se conservan pendientes vencimiento real del token y alternancia de servidores. Compilar Android no equivale a aprobar esa prueba. No se consultó ni modificó MonsterASP/db77593.

Bloque implementado y listo para revisión, sin commit, push, merge ni publicación. Mensaje sugerido: **feat: asignar baños y programar entregas de servicios nuevos**. Esperar confirmación antes del siguiente bloque.

Revisión final: EF confirmó que no hay cambios del modelo pendientes; entidades/configuraciones/migraciones sin diff. git diff --check correcto. 154 candidatos versionados/no ignorados examinados, sin generados, perfiles privados ni indicadores de credenciales/tokens/claves privadas en las rutas/textos revisados; índice vacío. PDF/PNG originales conservan SHA-256. Los cambios finales fueron documentales: no se repitieron pruebas ni compilaciones.

### Cierre: prueba manual Windows aprobada

El estudiante confirmó personalmente la **solicitud 4** en Windows, con API local y LocalDB: asignó los dos baños y completó la primera entrega, manteniéndose la solicitud **Aprobada**; al completar la segunda entrega, pasó a **Resuelta**. **Recorrido manual aprobado.** No se extiende esta confirmación a escenarios de permisos, concurrencia u otros controles no mencionados.

Esta evidencia manual se registra separada de los 9 casos nuevos y la suite automatizada de 134 casos anteriores (143 ejecuciones contando las focalizadas), y de las compilaciones ya documentadas. No se repitieron pruebas ni compilaciones: este cierre solo modifica documentación. La preparación Pendiente/sin servicios descrita arriba corresponde al estado previo a la prueba; se conserva el resultado final, sin liberar unidades ni restablecer tareas.

**Bloque cerrado y listo para commit.** Continúan pendientes la integral manual Android local al finalizar los bloques y, después, migraciones/republicación del hosting, además de vencimiento real del token y alternancia de servidores. Este bloque no agregó migraciones. Sin cambios en hosting, commit, push, merge ni inicio de otro bloque. Mensaje sugerido: **feat: asignar baños y programar entregas de servicios nuevos**.

## 22. Administrador: Baños y Servicios y retiro

### Revisión, rama y decisiones confirmadas (07/11/2026)

Master estaba limpio en ea92843 (merge PR #11), actualizado por el estudiante. Se creó feature/banos-servicios sin descartar cambios ni hacer pull/fetch adicional. Se revisaron AGENTS.md, texto funcional del PDF, página 6 renderizada de Figma, DER previamente inspeccionado, reglas C8/C9 del plan y código/modelo actual. Las tarjetas y navegación corresponden a Baños y Servicios de la referencia; los controles de empleado/fecha y revisión de tareas se mantienen dentro de esa vista.

Se consultaron las reglas no definidas y se adoptó la respuesta final del estudiante:

- Reutilizar una baja Pendiente/Aprobada existente, mostrando cuál se atiende; elegir explícitamente si hay varias. Sin ninguna, crear solo el retiro administrativo, sin inventar solicitud del cliente.
- Permitir programar el retiro. Antes de finalizar, mostrar otros trabajos para que administración decida explícitamente qué pendientes ya no corresponden y cancela, y cuáles conserva para completar. No se cancelan automáticamente ni se fuerzan limpiezas futuras innecesarias.
- En curso debe completarse antes del cierre. Un cumplimiento ya registrado tampoco se cancela como si no hubiera ocurrido. Los mantenimientos bloqueantes Pendientes se conservan y pueden continuar después del retiro, impidiendo disponibilidad; si están En curso, deben resolverse antes del cierre.

La propuesta inicial de exigir completar todos los pendientes sin opción de revisión no se adopta. La decisión final se representa por cancelaciones explícitas o conservación del estado actual, sin agregar estados ficticios ni modificar solicitudes silenciosamente.

### Modelo e implementación

**Sin cambios de entidades, configuraciones EF o migraciones nuevas**: se conservan las 12 tablas, relaciones e historial. Se reutilizan estados de tarea, FechaCancelacion, ClaveProgramacion, IdSolicitud y fechas del ciclo del servicio. No se escriben decisiones administrativas en ObservacionesEmpleado ni se inventa reposición de papel.

- ServiciosController y Contratos/Servicios: GET /api/servicios y /{id}, POST /{id}/retiro y /{id}/resolver-pendientes. Política Administrador/Activa, revalidada al escribir; empleado Empleado/Activa, servicio Entregado y ubicación actual esperada. Tarjetas con tipo/uso, cliente, dirección, frecuencia, estado, habitual actual del baño, empleados por tarea, solicitudes de baja y fechas. DTOs sin credenciales; orden estable y sin borrado de finalizados. Disponibilidad global por baño, considerando Apto, otra asignación vigente y mantenimiento activo.
- Programación de retiro deja servicio Entregado y asignación vigente, crea solo Retiro/Pendiente. Vincula y aprueba la baja elegida cuando existe; otras solicitudes no cambian. La inexistencia de solicitud no crea una ficticia. Misma clave/datos recupera el retiro; otra clave no genera un segundo. Se protege también SolicitudesController.Asignar para impedir duplicarlo desde esa ruta. Estados Finalizado/Cancelado/Pendiente de entrega no admiten esta programación; la cancelación antes de entrega queda para tratamiento separado, sin retiro ficticio.
- Resolver pendientes requiere servicio entregado con retiro programado y snapshot explícito de IDs a cancelar/conservar. Si aparecen trabajos nuevos o cambian estados, devuelve 409 antes de modificar. No permite cancelar En curso, cumplimiento registrado ni bloqueante. Cancelar cambia solo estado y FechaCancelacion, conserva contexto/empleado/solicitud/observaciones y es idempotente mientras el retiro siga activo; no cancela ni resuelve solicitudes. Lo conservado mantiene estado y debe completarse salvo mantenimiento bloqueante Pendiente, que puede continuar después.
- TareasController revalida al cerrar: bloquea retiro por otros trabajos Pendientes/En curso que requieren atención, conservando cumplimiento ya guardado para reintentar; no cancela automáticamente. Permite mantenimiento con servicio Finalizado sin desvincular historial. Retiro guarda FechaRetiroReal/FechaFinalizacion, estado Finalizado y libera la asignación; no cambia condición física ni otros bloqueos. La baja vinculada usa la resolución existente por trabajos requeridos, sin ignorar cancelados. Transacciones Serializable y bloqueo SQL común protegen programación, decisiones y cierres concurrentes.
- App/ContratosServicios, ClienteAutenticacion/SesionUsuario, BanosServicios.razor, Home/CSS: acceso habilitado, tarjetas, carga/vacío/error/consultar/Volver, Baja con empleado/fecha/solicitud y confirmación, revisión explícita en la tarjeta, estados recuperados y avisos solo después del guardado. No se confunde el habitual con el empleado de entrega/retiro. Mantiene local/USB/HTTPS y reglas de sesión. Sin otras funciones administrativas.

### Pruebas, recursos y compilación

Se conservaron las **134 pruebas anteriores** y se agregaron **13 casos nuevos** en ServiciosPruebas.cs para riesgos/reglas, sin nuevas pruebas visuales simples. Se ejecutaron:

- **20 casos afectados** en desarrollo (13 nuevos + 7 existentes), todos correctos.
- **Una única suite completa de cierre: 147 correctos, 0 fallidos y 0 omitidos**.
- Total **167 ejecuciones de casos**, con 147 casos distintos. No se repitieron por cambios exclusivamente documentales.

Cobertura nueva: retiro vinculado a baja, conservación del habitual/historial y fechas; sin liberación al programar; cancelación explícita e idempotente de limpieza futura sin resolver su solicitud; En curso/registro de cumplimiento protegido y cierre bloqueado hasta completarlo; mantenimiento bloqueante conservado después del retiro y completado después, sin volver Apto automáticamente; permisos en rutas nuevas, duplicados concurrentes y entre módulos, empleado/contexto inválido, elección entre varias bajas y revisión desactualizada sin cancelaciones ocultas.

Se ajustó únicamente la preparación auxiliar del caso existente de baja en SolicitudesPruebas: la tarea usada para obtener el contexto del fixture se prepara Cancelada cuando no representa un trabajo real pendiente del caso. No se eliminaron pruebas ni se cambiaron sus resultados esperados. Esta preparación se hace solo en su base aislada, no en trabajos locales existentes. TestServer y LocalDB usan bases nuevas BanosQuimicosAuthPruebas_<GUID>, conservadas.

API/pruebas compilaron en tmp/pruebas-servicios. App Debug Windows y Android compiló con **0 errores y 0 advertencias**, con parámetros temporales JavaMaximumHeapSize=512M/JavaOptions=-XX:+UseSerialGC ya documentados. Sin cambios de paquetes, herramientas o configuración global. No se ejecutó la interfaz nativa; pruebas manuales Windows pendientes. Reiniciar la API con esta versión antes de probar.

### Datos locales y guía

Se ejecutó scripts/preparar-banos-servicios-local.sql solo en (localdb)\MSSQLLocalDB/BanosQuimicosDb tras verificar IsLocalDB=1 y conteos. Crea registros dedicados de prueba, sin modificar cuentas/contraseñas/trabajos anteriores:

- Servicio **4 / PRUEBA-RETIRO-01**, Entregado/Apto, baja Pendiente y tarea **7 Limpieza semanal** futura, para decidir explícitamente si se cancela.
- Servicio **5 / PRUEBA-RETIRO-02**, Entregado/Apto, sin baja de cliente y tarea **8 Mantenimiento bloqueante** Pendiente vinculada al servicio, para comprobar que continúa después del retiro.

No se crearon sus tareas de retiro: quedan para la confirmación manual. Los nuevos trabajos usan el empleado de prueba existente IdUsuario 1004 sin cambiarlo. Antes: 6 usuarios, 3 baños/3 servicios/6 tareas/4 solicitudes. Después: mismos 6 usuarios y datos anteriores, con 5 baños/5 servicios/8 tareas/5 solicitudes. El script reutiliza sus registros sin reabrirlos ni restablecer estados. No se contactó ni modificó MonsterASP/db77593.

Guía en docs/banos-servicios.md: administrador programa y revisa pendientes → empleado realiza retiro → servicio Finalizado y disponibilidad según condición/mantenimiento → solicitud vinculada actualizada cuando se cumplen sus trabajos. El escenario bloqueante permite comprobar mantenimiento posterior al retiro, sin cancelar reparaciones por omisión.

### Pendientes y cierre

Prueba manual Windows de ambos recorridos aprobada según el cierre siguiente. Integral manual Android al terminar bloques contra API local; después actualización/migraciones pendientes del hosting y republicación. Se mantienen pendientes vencimiento real del token y alternancia de servidores. Este bloque no agrega migraciones ni publica.

Bloque implementado y listo para revisión, sin commit, push, merge ni inicio de otro bloque. Mensaje sugerido: **feat: gestionar servicios y programar retiros con revisión de pendientes**. Esperar confirmación antes de continuar.

Revisión final: EF confirmó modelo sin cambios pendientes; entidades/configuraciones/migraciones sin diff. git diff --check correcto. Se revisaron diff/archivos nuevos y 162 candidatos no ignorados, sin generados, perfiles privados ni indicadores de credenciales/tokens/claves privadas en los textos/rutas examinados; índice vacío. PDF y PNG originales conservan SHA-256. Las últimas modificaciones fueron documentales: no se repitieron pruebas ni compilaciones.

### Cierre: pruebas manuales Windows aprobadas

El estudiante confirmó ambos recorridos con API local y LocalDB:

- **Primer retiro aprobado:** Actualizar Estado impidió finalizar mientras existía una limpieza pendiente. El administrador canceló explícitamente esa limpieza y confirmó la revisión; después el empleado pudo finalizar el retiro. Se confirmó el resto del recorrido previsto de ese caso.
- **Retiro con mantenimiento bloqueante aprobado:** se completó el retiro conservando el mantenimiento y el bloqueo de disponibilidad del baño después del cierre.

Estas evidencias son **manuales** y se registran separadas de las **147 pruebas automatizadas anteriores**, de las 20 ejecuciones afectadas previas y de las compilaciones. No se atribuye a esta confirmación la reparación posterior opcional, ni otros escenarios no mencionados. No se repitieron pruebas ni compilaciones: este cierre solo modifica documentación.

Se conservan los resultados guardados como historial; no se restablecen servicios, solicitudes o tareas ni se libera un mantenimiento por fuera del recorrido. Los estados iniciales de los ejemplos descritos arriba corresponden a la preparación anterior a la prueba.

**Bloque cerrado y listo para commit.** La integral Android local y la posterior actualización/republicación del hosting siguen para el final de los bloques. Permanecen pendientes vencimiento real del token y alternancia de servidores. Sin migración nueva, cambios en hosting, commit, push, merge ni inicio de otro bloque. Mensaje sugerido: **feat: gestionar servicios y programar retiros con revisión de pendientes**.

## 23. Consulta de servicios del cliente

### Alcance, referencias y reglas

Master estaba limpio en `7f7df27` (merge de Baños y Servicios), actualizado por el estudiante. Se creó `feature/servicios-cliente` desde esa versión, conservando todos los datos y sin descartar cambios. Revisión de AGENTS.md, documentación funcional del PDF, página 7 visual, DER/modelo, decisiones del plan y las cuatro capturas aportadas. No se modifica login ni el identificador por correo electrónico.

Recorrido implementado: **Inicio del cliente → Mis Servicios → Detalle → Volver al listado → Volver al inicio**, con cierre de sesión existente. Inicio con nombre real de `/yo`, rol Cliente y contador consultado a la API; tarjetas con código, tipo de baño/uso, dirección y frecuencia. Detalle con los mismos datos y estado. Incluye carga, lista vacía, errores y consulta nuevamente; una consulta fallida nunca se presenta como cero servicios.

**Vigentes = Pendiente de entrega + Entregado**, conforme C8 y la exclusividad de asignación existente. El contador y listado usan la misma regla. Finalizado/Cancelado permanecen en el historial pero no aparecen en esta consulta. Solicitar o programar retiro no elimina la vigencia. Condición física y mantenimiento no ocultan un servicio aún asignado. Dirección del propio servicio, actual o preparada si todavía no se entregó, sin tomar ubicación de otro servicio del mismo baño. La frecuencia continúa siendo referencia manual.

Se mantienen visibles y deshabilitados Mis Solicitudes, Solicitar un nuevo servicio y acciones de limpieza extra, cambio de dirección, reclamo y baja. Las modalidades «Ya realicé el cambio de dirección» y «Necesito que lo trasladen» siguen diferenciadas, sin implementar sus formularios. Sin mensajes de éxito ni operaciones simuladas. Se respetan tarjetas, colores y navegación de las capturas y se reutiliza la marca existente: no hay un recurso independiente del logo Celeris en el repositorio y no se inventó otro.

### API, modelo y archivos

- `Controllers/ServiciosClienteController.cs`: GET `/api/servicios-cliente/mios/resumen`, `/mios` y `/{id}`. Filtrado por usuario autenticado mediante CLIENTES; no se recibe idCliente. Orden estable código/ID. Detalle ajeno, inexistente o que dejó de estar vigente devuelve 404. DTO mínimo sin datos administrativos privados ni credenciales, respuesta sin caché.
- `Seguridad/Politicas.cs` y `ConfiguracionSeguridad.cs`: política Cliente/Activa. La autenticación existente consulta rol/estado actuales en cada petición; empleado, administrador y cuenta pendiente no acceden a las consultas del cliente.
- App: `Autenticacion/ContratosServiciosCliente.cs`, `ClienteAutenticacion.cs` y `SesionUsuario.cs` reutilizan rutas internas, token por petición y guardas de sesión/rol existentes. `Components/Clientes/InicioCliente.razor`, `MisServicios.razor`, `Pages/Home.razor` y `wwwroot/app.css` agregan las vistas autorizadas, navegación y presentación. Al volver se recargan listado/contador; no se conservan datos de otro usuario al cerrar sesión.
- Proyecto de pruebas enlaza los componentes nuevos para compilar Home; no se añadieron pruebas visuales simples. `ServiciosClientePruebas.cs` cubre reglas y aislamiento. README y `docs/servicios-cliente.md` contienen orientación y guía manual.

**Sin cambios de entidades, configuraciones EF ni migraciones**; se conservan las 12 tablas e historial. No se escribieron registros en `BanosQuimicosDb`. Solo consultas de lectura para elegir datos de prueba; las escrituras automatizadas utilizan bases nuevas aisladas `BanosQuimicosAuthPruebas_<GUID>`, conservadas sin borrar datos anteriores. Conexión local existente sin cambios: Windows HTTPS localhost:7292; Android Debug loopback:5148 por USB. Hosting no consultado ni modificado.

### Verificaciones y preparación manual

Se agregaron **8 casos automatizados**: aislamiento entre dos clientes tanto en lista como en detalle, intento de elegir otra identidad por query, resumen coherente con vigencia, dirección preparada, exclusión de finalizados/cancelados, actualización al cerrar servicio conservando historia, cliente sin servicios y las tres rutas ante anónimo/administrador/empleado/pendiente/suspendido/baja lógica. Se conservaron las 147 pruebas anteriores.

Primera ejecución afectada: 7 correctos y 1 fallido por traducción EF del filtro de detalle después de proyectar el DTO. Se corrigió moviendo el filtro de ID antes de la proyección; segunda ejecución: **8 correctos**. El fallo ocurrió en una base aislada y no modificó datos operativos. **Una única suite completa de cierre: 155 correctos, 0 fallidos y 0 omitidos** (147 existentes + 8 nuevos). Total **171 ejecuciones de casos**, contando ambas pasadas focalizadas y el fallo corregido; 155 casos distintos. No se repiten pruebas ni compilaciones por los cambios documentales del cierre.

App Debug Windows y Android compiló con **0 errores y 0 advertencias**. Parámetros temporales JavaMaximumHeapSize=512M y JavaOptions=-XX:+UseSerialGC, sin cambios de paquetes ni configuración global. API y componentes enlazados compilaron con las pruebas en `tmp/pruebas-cliente`. No se ejecutó ni se atribuye comprobación visual a la app nativa.

Consulta de preparación solo lectura verificó `DB_NAME() = BanosQuimicosDb` e `IsLocalDB = 1`. Cliente Prueba (IdUsuario 2) tiene **3 servicios vigentes**: PRUEBA-TAREAS-01, PRUEBA-NUEVO-01 y PRUEBA-NUEVO-02. Los servicios 4/5 de retiro están Finalizados. Cuentas **Empleado Prueba** (IdUsuario 3, rol Cliente pese al nombre) y **Agus Tin** (IdUsuario 5) están Activas y sin servicios. Usar sus credenciales conocidas sin cambiarlas; si no se conocen, registrar personalmente otra cuenta y activarla con el administrador existente. No se documentan contraseñas ni tokens.

### Pendientes y cierre para revisión

Guía reproducible Windows en `docs/servicios-cliente.md`: API local HTTPS actualizada → cuenta con servicios → tarjetas/detalle/Volver → acciones deshabilitadas → cuenta sin servicios → pérdida/recuperación de conexión → cierre de sesión. **Recorrido manual Windows aprobado en el cierre siguiente**, únicamente para los casos confirmados. Integral Android local al terminar los bloques; después actualización de migraciones previas y republicación de API. Permanecen pendientes vencimiento real del token y alternancia de servidores en dispositivo. Solicitudes y acciones del detalle del cliente continúan en bloques posteriores.

**Bloque de consulta cerrado y listo para commit**, con los recorridos Windows confirmados; no comenzar el siguiente sin confirmación. Mensaje sugerido: **feat: consultar servicios propios del cliente**. Sin commit, push, merge ni publicación.

Revisión final: diff sin errores de espacios; entidades/configuraciones/migraciones y conexión local sin cambios. Índice Git vacío. Se revisaron los 168 archivos candidatos no ignorados: sin generados, perfiles privados ni indicadores de credenciales, JWT o claves privadas en las rutas/textos examinados. PDF y dos PNG originales conservan sus SHA-256. Los cambios finales son documentales y no requieren repetir pruebas o compilaciones.

### Cierre: pruebas manuales Windows aprobadas

El estudiante confirmó el recorrido con API local y LocalDB:

- **Cliente Prueba:** contador de 3 servicios, listado, detalle y navegación de regreso correctos.
- **Cliente sin servicios:** mensaje de listado vacío, sin datos del cliente anterior.
- **Acciones futuras:** visibles y deshabilitadas.

Evidencia **manual**, separada de las **155 pruebas automatizadas anteriores** y de las compilaciones ya registradas. No se consideran confirmados por este mensaje los pasos adicionales de desconexión/recuperación, reinicio, cierre persistente ni ventana reducida. Solo se actualizaron el plan y la guía; no se repitieron pruebas ni compilaciones y no se modificaron datos.

**Bloque cerrado y listo para commit.** Integral manual Android contra API local y posterior republicación/migraciones pendientes del hosting reservadas para el final. Se conservan los pendientes de vencimiento real del token y alternancia de servidores. Sin commit, push, merge, cambios en hosting ni inicio de otro bloque. Mensaje sugerido: **feat: consultar servicios propios del cliente**.

## 24. Solicitar nuevo servicio y Mis Solicitudes: revisión inicial

Master limpio en `d47b023` (merge PR #13), actualizado por el estudiante. Se creó `feature/solicitudes-cliente` desde esa versión, sin descartar cambios. Revisados AGENTS.md, decisiones del plan, referencias del cliente en PDF/capturas, modelo/configuración de SOLICITUDES_CLIENTE, contratos y recorrido administrativo de asignación/entregas existentes.

El alcance autorizado es crear pedidos nuevos Pendientes del cliente autenticado y consultar todos sus tipos de solicitudes, sin crear servicios ni tareas al enviar, sin edición/cancelación/eliminación y sin modificar hosting. Aprobada y Resuelta siguen siendo estados distintos; las entregas parciales no resuelven el pedido. Se conservarán las 12 tablas, cuentas y trabajos existentes.

**Decisión funcional pendiente antes del formulario:** Figma contiene un único selector «Tipo de Baño», mientras el modelo y la regla aprobada separan el tipo físico solicitado (Obra/Evento) del uso previsto (Obra/Evento), permitiendo combinaciones cruzadas. La asignación administrativa exige ambos datos y no los deduce. Propuesta a confirmar: mantener el selector de tipo y agregar en la misma pantalla un selector «Uso del servicio», sin asumir que coinciden. No se considera una decisión confirmada ni se implementó aún.

**Ajuste técnico previsto, todavía no aplicado:** SOLICITUDES_CLIENTE no tiene una clave persistente de envío. Para reconocer reintentos después de un guardado cuya respuesta se perdió, se propone agregar `ClaveEnvio` (GUID opcional para registros anteriores) con índice único filtrado. La app conservaría la misma clave y contenido durante el reintento; la API devolvería la solicitud ya creada, sin duplicarla, y rechazaría la misma clave con contenido distinto. No se usa Descripcion como campo técnico ni se deduplican silenciosamente pedidos legítimos de igual contenido. Esto requiere una migración aditiva sin tablas nuevas ni modificación de datos existentes; se explica antes de modificar el modelo/aplicarla. Su aplicación al hosting quedaría pendiente para la publicación futura.

En esta revisión solo cambió el plan. No se implementaron pantallas/endpoints ni se generó/aplicó migración; no corresponde ejecutar pruebas o compilaciones todavía. Bloque abierto a la definición del dato obligatorio. Integral Android local y republicación siguen reservadas para el final. Sin commit, push, merge ni cambios en hosting.

### Decisiones confirmadas e implementación

El estudiante autorizó **Uso del servicio Obra/Evento separado de Tipo de baño solicitado**, dentro del mismo formulario, y actualizará Figma. Se conserva tipo Obra para uso Evento. También autorizó la migración aditiva para ClaveEnvio opcional y el índice único filtrado, solo en LocalDB. Las propuestas anteriores de esta sección quedan confirmadas por esta respuesta, sin modificar los originales.

- `Contratos/Solicitudes/NuevoServicioSolicitud.cs` y `Controllers/SolicitudesClienteController.cs`: GET tipos-bano, mias y detalle propio, POST nuevo-servicio bajo `/api/solicitudes-cliente`. Cliente/Activa, identidad desde sesión, sin parámetro de cliente confiable; detalle ajeno 404, clave ajena/alterada 409 sin revelar datos. Solicitud tipo Nuevo servicio/Pendiente y reloj del servidor; sin servicios ni tareas al enviar. Lista de todos los tipos propios con descripción, resumen de pedido, estado real y códigos de servicios realmente vinculados. DTOs sin claves de acceso ni datos administrativos privados. No hay edición/cancelación/borrado.
- Validación API/app: dirección requerida, trim y hasta 300 caracteres; cantidad positiva dentro del entero existente; tipo existente del catálogo; uso Obra/Evento; fecha válida no omitida. La fecha deseada es referencia, no reserva; no existe regla confirmada que exija futuro y no se inventó. Frecuencia de limpieza, empleado y fecha de entregas siguen siendo decisiones administrativas ya implementadas.
- Envíos: transacción Serializable, bloqueo SQL e índice único; mismo cliente/clave/contenido normalizado recupera la solicitud (200, incluso tras resolver); primera creación 201. Otra clave genera un pedido nuevo, aunque el contenido coincida. No se usa Descripcion para datos técnicos ni se deduplica por contenido. La app impide doble toque, conserva borrador/clave en la sesión en memoria incluso al desmontar por error de conexión y bloquea modificar el contenido mientras el resultado es incierto; reintenta el mismo envío. Validación 400 permite corregir. Guardado confirmado permite iniciar otro pedido con clave nueva. El borrador no se guarda en disco y se limpia al cerrar sesión: tras reinicio con resultado incierto consultar Mis Solicitudes antes de crear otro pedido.
- App `ContratosSolicitudesCliente`, `ClienteAutenticacion`, `SesionUsuario`, `NuevoServicio.razor`, `MisSolicitudes.razor`, InicioCliente/Home/CSS: accesos habilitados, formulario según referencia con adaptación autorizada, confirmación real, tarjetas/indicadores con texto y color, carga/vacío/error/actualizar/Volver. Aprobada no equivale a Resuelta; no se inventa baño antes de asignar. Se reutilizan reglas de sesión y API local; acciones del detalle del servicio siguen pendientes. Guía `docs/solicitudes-cliente.md` y enlace en README.

### Migración y conservación de datos

**`20261107191916_ClaveEnvioSolicitud` aplicada exclusivamente en (localdb)\MSSQLLocalDB/BanosQuimicosDb**: columna `SOLICITUDES_CLIENTE.ClaveEnvio uniqueidentifier NULL` e índice único `IX_SOLICITUDES_CLIENTE_ClaveEnvio` con filtro `[ClaveEnvio] IS NOT NULL`. Sin tablas nuevas, cambios de catálogos, borrados ni asignación de claves artificiales a pedidos anteriores. Snapshot y Designer reflejan solo el cambio autorizado. Down no se ejecutó.

Verificaciones SQL antes/después: LocalDB=1, base correcta, **12 tablas de negocio**, 27 filas de catálogos, 6 usuarios, 5 solicitudes, 5 servicios y 11 tareas conservados; las cinco solicitudes anteriores siguen sin clave. La tabla adicional `__EFMigrationsHistory` es técnica de EF y registra la migración, no forma parte de las 12 del negocio. Primer intento con `--no-build` encontró binario anterior a generar la migración y EF rechazó diferencias antes de aplicar cambios; se recompiló y actualizó correctamente, sin suprimir la validación.

**Hosting pendiente**: revisar historial remoto y aplicar esta migración, junto con las anteriores pendientes que correspondan, antes de republicar la API al final tras validación integral Android local. No se contactó ni modificó MonsterASP/db77593.

### Verificaciones y guía local

Se conservan las **155 pruebas anteriores**. Se agregaron **11 casos** en SolicitudesClientePruebas: registro real sin servicios/tareas, uso cruzado, recorrido completo y entregas parciales, reintento tras resolución, reintentos simultáneos/clave alterada/ajena/nueva, aislamiento de lista/detalle y escritura administrativa, validaciones, solicitudes de otros tipos/estados y múltiples claves nulas, seis casos de permisos y borrador de app. Compilación inicial de pruebas requirió corregir un alias; no ejecutó casos. **Pruebas nuevas afectadas: 11/11 correctas**. **Una única suite completa de cierre: 166 correctos, 0 fallidos y 0 omitidos**. Total **177 ejecuciones de casos**, 166 casos distintos. Sin nuevas pruebas visuales simples ni repetición por cambios documentales. Las escrituras de pruebas usan bases LocalDB nuevas aisladas y conservadas, sin alterar cuentas operativas.

Compilación App Debug **Windows y Android correcta, 0 errores y 0 advertencias**, con parámetros temporales de Java ya utilizados, sin instalar/actualizar paquetes o herramientas. API y Razor enlazados compilaron en tmp/pruebas-solicitudes-cliente. No se ejecutó la interfaz nativa ni se atribuye prueba manual a la compilación.

No hizo falta preparar datos nuevos: lectura local confirmó **PRUEBA-RETIRO-01 (ID 4)** y **PRUEBA-RETIRO-02 (ID 5)** como Apto/tipo Obra y disponibles, sin asignación vigente ni mantenimiento bloqueante activo en ese momento. No se restableció ni modificó su historial. El cliente creará personalmente el pedido nuevo desde la app; no se creó una solicitud ficticia por SQL. Reutilizar Cliente Prueba y PRUEBA CUENTAS Windows con sus credenciales conocidas, sin cambiar roles ni contraseñas.

Guía Windows: cliente envía pedido de 2 baños tipo Obra/uso Evento → confirma Pendiente sin baños → administrador asigna las dos unidades, empleado/fecha/frecuencia → Aprobada y nuevos servicios pendientes → empleado registra/finaliza primera entrega → cliente sigue viendo Aprobada → segunda entrega → Resuelta y servicios Entregado. Recorrido completo manual Windows aprobado según el cierre siguiente, únicamente para lo confirmado por el estudiante. Integral Android local al terminar bloques; después migraciones y republicación. Permanecen pendientes vencimiento real del token y alternancia de servidores. No comenzar otro bloque sin confirmación. Mensaje sugerido: **feat: solicitar nuevos servicios y consultar solicitudes del cliente**. Sin commit, push, merge ni cambios en hosting.

**Bloque cerrado y listo para commit**, con recorrido manual Windows aprobado. Diff revisado y sin errores de espacios; 177 archivos candidatos no ignorados sin generados, perfiles privados ni indicadores de secretos en las rutas/textos examinados. Índice vacío y originales PDF/PNG con sus hashes conservados. La revisión final solo modifica documentación: no se repiten pruebas ni compilaciones. Esperar confirmación antes de otro bloque.

### Cierre: recorrido manual Windows aprobado

El estudiante confirmó con API local y LocalDB: cliente envía pedido y lo ve **Pendiente** → administrador asigna baños y entregas → empleado completa las entregas → cliente ve solicitud **Resuelta**, nuevos servicios y contador actualizado.

Verificación **manual**, separada de las **166 pruebas automatizadas anteriores** y de las compilaciones registradas. No se atribuyen a esta confirmación validaciones de campos, reintentos, aislamiento entre cuentas, pérdida/recuperación de conexión ni consulta entre entregas parciales. Los datos iniciales de la guía corresponden a la preparación previa; no se modifican ni restablecen cuentas, unidades, solicitudes, servicios o tareas en este cierre.

Se actualizaron únicamente el plan y la guía. No se repitieron pruebas ni compilaciones por cambios documentales. **Bloque cerrado y listo para commit.** `20261107191916_ClaveEnvioSolicitud` sigue aplicada solo en LocalDB y pendiente para el hosting. Integral manual Android local y posterior actualización/republicación reservadas para el final. Continúan pendientes vencimiento real del token y alternancia de servidores. Sin commit, push, merge, cambios en hosting ni inicio de otro bloque. Mensaje sugerido: **feat: solicitar nuevos servicios y consultar solicitudes del cliente**.
