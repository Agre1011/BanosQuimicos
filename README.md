# Baños Químicos · Celeris

Proyecto de Programación Móvil para gestionar baños químicos, servicios, solicitudes y trabajos. Sin pagos ni facturación.

- **BanosQuimicos.App:** .NET 10 MAUI Blazor Hybrid, componentes Razor, Windows y Android.
- **BanosQuimicos.Api:** ASP.NET Core Web API con controladores, EF Core y SQL Server.
- **BanosQuimicos.Api.Pruebas:** integración con bases LocalDB aisladas.

-Despliegue. La API REST está alojada en MonsterASP, en https://celeris.runasp.net/, con la base de datos SQL Server en el mismo proveedor. Para verificar que el servicio está activo puede consultarse https://celeris.runasp.net/api/estado. La aplicación móvil se conecta a esta dirección cuando está configurada para usar el hosting.

Abrir `BanosQuimicos.sln` en Visual Studio 2026. Preparación y ejecución: [ejecución local](docs/ejecucion-local.md).

## Documentación vigente

- [Plan y pendientes](docs/plan-trabajo.md) y [alcance confirmado](docs/alcance-y-reglas.md).
- Guías de [administrador](docs/administrador.md), [empleado](docs/empleado.md) y [cliente](docs/cliente.md).
- [Base de datos](docs/base-datos.md), [LocalDB](docs/localdb.md) y [autenticación](docs/autenticacion-api.md).
- [Verificaciones y límites](docs/verificaciones.md).
- [Publicación y migraciones pendientes](docs/publicacion.md).

Los documentos aprobados originales permanecen en `docs`, sin modificaciones. Las guías anteriores y el plan cronológico están en el [archivo histórico](docs/historico/indice.md); describen su etapa, no necesariamente el comportamiento actual.
