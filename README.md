# Baños Químicos

Proyecto en .NET 10 con persistencia EF Core/SQL Server y autenticación de la API. Todavía sin controladores CRUD del negocio ni integración de autenticación en la app.

- `BanosQuimicos.Api`: ASP.NET Core Web API con controladores.
- `BanosQuimicos.App`: .NET MAUI Blazor Hybrid con componentes Razor. Destinos Android y Windows; Windows permite comprobar la interfaz en la PC.
- `BanosQuimicos.Api.Pruebas`: pruebas de integración con JWT y SQL Server reales, en bases aisladas de prueba.
- `docs`: documentación original y plan de trabajo.

La app todavía no consume la API. Esta etapa permite comprobar cada proyecto por separado.

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

1. Detener la depuración de la API si está activa y establecer `BanosQuimicos.App` como proyecto de inicio.
2. Para una comprobación local en Windows, seleccionar `Windows Machine` y presionar F5. Debe aparecer el título «Baños Químicos» y una breve descripción.
3. La pantalla está en `Components/Pages/Home.razor`; `MainPage.xaml` hospeda el BlazorWebView y `Components/Routes.razor` resuelve la ruta inicial.

El estudiante ya probó la pantalla inicial en Windows y en un teléfono Android físico por USB. Para repetir en Android, seleccionar ese teléfono en Visual Studio y ejecutar. Continúa pendiente el título parcialmente tapado por la franja superior y la conexión app/API.

## Compilar desde terminal

```powershell
dotnet sln BanosQuimicos.sln list
dotnet build BanosQuimicos.sln
```

La solución incluye dos proyectos de aplicación y un proyecto de pruebas. El proyecto MAUI compila para Android y Windows. Los archivos de plataforma iOS/MacCatalyst que conserva la plantilla no son destinos habilitados en esta etapa.

## Git

El estudiante realizó los commits, push y merge previos. El bloque actual se desarrolla en `feature/autenticacion-api`, sin commit ni push del agente. `.gitignore` excluye, entre otros, `bin`, `obj`, `.vs` y archivos personales de Visual Studio. La documentación original se conserva.

## Base de datos

La base local es `BanosQuimicosDb` en `(localdb)\MSSQLLocalDB`, con autenticación de Windows. La API registra el DbContext, pero no ejecuta migraciones automáticamente al iniciar. Ver [modelo, restricciones y pasos para consultar la base](docs/base-datos.md).

Ver `docs/plan-trabajo.md` para evidencias de esta entrega, limitaciones y próximos pasos.
