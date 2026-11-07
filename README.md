# Baños Químicos

Proyecto en .NET 10 con persistencia EF Core/SQL Server y autenticación en la API y la app MAUI. Todavía sin controladores CRUD del negocio.

- `BanosQuimicos.Api`: ASP.NET Core Web API con controladores.
- `BanosQuimicos.App`: .NET MAUI Blazor Hybrid con componentes Razor. Destinos Android y Windows; Windows permite comprobar la interfaz en la PC.
- `BanosQuimicos.Api.Pruebas`: pruebas de integración con JWT y SQL Server reales, en bases aisladas de prueba.
- `docs`: documentación original y plan de trabajo.

La app consume registro, login y consulta de cuenta de la API. Ver [guía de autenticación de la app](docs/autenticacion-app.md) para Windows y teléfono Android físico por USB.

## Abrir y compilar en Visual Studio 2026

1. Abrir `BanosQuimicos.sln` con Archivo > Abrir > Proyecto o solución.
2. Esperar la restauración de paquetes NuGet.
3. Elegir configuración Debug y Compilar > Compilar solución.

Antes de ejecutar la API por primera vez, ejecutar `./scripts/configurar-jwt-local.ps1` desde PowerShell para guardar una clave aleatoria en User Secrets, fuera de Git. Ver [autenticación y pruebas](docs/autenticacion-api.md) para crear el primer administrador sin credenciales fijas.

Guías: [flujo completo en Postman](docs/guia-postman-autenticacion.md) y [LocalDB por nombre normal](docs/localdb.md).

Se necesita el SDK .NET 10 y las herramientas MAUI/Android. La instalación o actualización de componentes requiere revisión previa.

## Ejecutar la API

1. En el Explorador de soluciones, clic derecho sobre `BanosQuimicos.Api` > Establecer como proyecto de inicio.
2. Seleccionar el perfil `http` y presionar F5 (depurar) o Ctrl+F5 (sin depurar).
3. Abrir `http://localhost:5148/api/estado`, o enviar la petición de `BanosQuimicos.Api.http` desde Visual Studio.
4. Debe responder HTTP 200 con este JSON:

```json
{"mensaje":"La API de Baños Químicos está funcionando."}
```

La raíz `/` no tiene una página y puede responder 404. En esta etapa HTTP está limitado al uso local de desarrollo; no se configura publicación.

El archivo `Program.cs` registra los controladores y conecta sus rutas. `Controllers/EstadoController.cs` recibe GET `/api/estado` y devuelve el mensaje.

Alternativa desde una terminal en la raíz:

```powershell
dotnet run --project BanosQuimicos.Api --launch-profile http
```

## Ejecutar la app

1. Mantener la API ejecutándose con el perfil `https` en otra terminal y establecer `BanosQuimicos.App` como proyecto de inicio. También se admite inicio múltiple en Visual Studio.
2. Para una comprobación local en Windows, seleccionar `Windows Machine` y presionar F5. Debe aparecer el título «Baños Químicos» y el formulario de inicio de sesión.
3. La pantalla está en `Components/Pages/Home.razor`; `MainPage.xaml` hospeda el BlazorWebView y `Components/Routes.razor` resuelve la ruta inicial.

El estudiante confirmó las pruebas manuales de autenticación en Windows y en su Moto G9 Plus físico por USB, incluidos restauración/cierre de sesión, recuperación de conexión y presentación con teclado y rotación. El pendiente visual del título quedó resuelto. Android Debug usa `adb reverse tcp:5148 tcp:5148`, detallado en la guía de la app. El vencimiento real del token sigue pendiente; ver el registro separado de pruebas manuales y automatizadas en el plan.

## Compilar desde terminal

```powershell
dotnet sln BanosQuimicos.sln list
dotnet build BanosQuimicos.sln
```

La solución incluye dos proyectos de aplicación y un proyecto de pruebas. El proyecto MAUI compila para Android y Windows. Los archivos de plataforma iOS/MacCatalyst que conserva la plantilla no son destinos habilitados en esta etapa.

## Git

El estudiante realizó los commits, push y merge previos. El bloque actual se desarrolla en `feature/solicitudes-cliente`, sin commit ni push en este bloque. `.gitignore` excluye, entre otros, `bin`, `obj`, `.vs` y archivos personales de Visual Studio. La documentación original se conserva.

Cliente: [guía de Mis Servicios](docs/servicios-cliente.md) y [solicitar nuevo servicio y Mis Solicitudes](docs/solicitudes-cliente.md). Las acciones del detalle del servicio (limpieza extra, dirección, reclamo y baja) siguen deshabilitadas. El formulario de nuevo servicio y Mis Solicitudes están habilitados para probar localmente.

## Gestión de cuentas

El administrador puede consultar cuentas con búsqueda, filtro y paginación, ver sus detalles y activar clientes pendientes con confirmación. Ver [guía y resultados esperados](docs/gestion-cuentas.md). No se incluyen edición, suspensión ni baja. El cambio Cliente ↔ Empleado se incorpora en el bloque de acceso de empleados, descrito más abajo.

## Base de datos

La base local es `BanosQuimicosDb` en `(localdb)\MSSQLLocalDB`, con autenticación de Windows. La API registra el DbContext, pero no ejecuta migraciones automáticamente al iniciar. Ver [modelo, restricciones y pasos para consultar la base](docs/base-datos.md).

Ver `docs/plan-trabajo.md` para evidencias de esta entrega, limitaciones y próximos pasos.

## Acceso de empleados

El usuario se registra como Cliente/Pendiente. Un administrador activo puede cambiar Cliente ↔ Empleado desde Clientes y Empleados, conservando la cuenta y sin modificar su estado. Este bloque usa API local/LocalDB; ver [recorrido y pruebas](docs/acceso-empleados.md). No se ha publicado el endpoint nuevo.

## Consulta de tareas del empleado

Contador real de hoy, listado y detalle propios con regreso según Figma. API local/LocalDB: [guía y tarea de prueba identificable](docs/tareas-asignadas-empleado.md). [Registrar Trabajo y Actualizar Estado](docs/registrar-trabajo.md) se realizan en el mismo detalle, según la adaptación autorizada, con persistencia independiente y cierre posterior.

## Estado Baños y programación manual

El administrador asigna responsable habitual y programa cada limpieza por fecha, pudiendo elegir otro empleado para esa tarea. No hay generación semanal automática. [Guía local administrador → empleado](docs/estado-banos.md), con migración y próximo recorrido de servicio nuevo documentados. Sin publicación en hosting.

## Solicitudes y Reclamos

Listado/detalle administrativos, aprobación y asignación puntual con tareas vinculadas, sin cambiar responsables habituales. [Guía Windows y datos locales](docs/solicitudes-reclamos.md). Servicio nuevo tiene [selección de baños y entregas](docs/asignar-banos-servicio.md); baño adicional, traslado informado y respuesta sin trabajos conservan su tratamiento separado. La prueba integral Android se hará al terminar los bloques y contra API local; después se planificará la republicación.

## Baños y Servicios

Tarjetas e historial, retiro programado sin liberar anticipadamente la unidad y revisión explícita de tareas pendientes. El cierre del empleado finaliza el servicio y calcula disponibilidad respetando condición y mantenimiento. [Guía Windows con dos servicios de prueba](docs/banos-servicios.md). Sin migración nueva ni publicación en hosting.
