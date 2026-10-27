# Baños Químicos

Estructura inicial en .NET 10, sin entidades, autenticación ni persistencia.

- `BanosQuimicos.Api`: ASP.NET Core Web API con controladores.
- `BanosQuimicos.App`: .NET MAUI Blazor Hybrid con componentes Razor. Destinos Android y Windows; Windows permite comprobar la interfaz en la PC.
- `docs`: documentación original y plan de trabajo.

La app todavía no consume la API. Esta etapa permite comprobar cada proyecto por separado.

## Abrir y compilar en Visual Studio 2026

1. Abrir `BanosQuimicos.sln` con Archivo > Abrir > Proyecto o solución.
2. Esperar la restauración de paquetes NuGet.
3. Elegir configuración Debug y Compilar > Compilar solución.

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

Para Android, primero confirmar con el estudiante teléfono físico o emulador. Luego verificar su configuración, seleccionar ese destino en Visual Studio y ejecutar. Compilar Android no implica haber instalado o probado la app en un dispositivo.

## Compilar desde terminal

```powershell
dotnet sln BanosQuimicos.sln list
dotnet build BanosQuimicos.sln
```

La solución incluye exactamente los dos proyectos. El proyecto MAUI compila para Android y Windows. Los archivos de plataforma iOS/MacCatalyst que conserva la plantilla no son destinos habilitados en esta etapa.

## Git

Repositorio inicializado solo localmente. `.gitignore` excluye, entre otros, `bin`, `obj`, `.vs` y archivos personales de Visual Studio. La documentación original permanece disponible para versionar. No se han realizado commits ni configurado publicación.

Ver `docs/plan-trabajo.md` para evidencias de esta entrega, limitaciones y próximos pasos.
