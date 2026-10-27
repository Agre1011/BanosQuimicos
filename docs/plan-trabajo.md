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
