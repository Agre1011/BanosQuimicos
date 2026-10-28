# Modelo y persistencia

La API contiene las 12 entidades acordadas. La app no se conecta directamente a SQL Server y todavía no consume la API. El bloque posterior agregó [autenticación de la API](autenticacion-api.md) sin cambiar el esquema; todavía no hay CRUD del negocio.

## Archivos para estudiar

- `BanosQuimicos.Api/Modelos`: una clase por entidad, con propiedades y referencias de navegación.
- `BanosQuimicos.Api/Datos/BanosQuimicosDbContext.cs`: reúne los conjuntos de entidades, carga sus configuraciones y establece relaciones sin borrado en cascada.
- `BanosQuimicos.Api/Datos/Configuraciones`: una configuración por entidad. Define nombres de tablas, claves, longitudes, campos opcionales, índices, restricciones y datos de catálogos.
- `BanosQuimicos.Api/Datos/Migraciones`: migración Inicial, metadatos y snapshot. El snapshot describe el último modelo migrado; no es una copia de los datos.
- `BanosQuimicos.Api/Program.cs`: registra el contexto con `UseSqlServer`. No migra ni crea bases automáticamente al iniciar.
- `BanosQuimicos.Api/appsettings.Development.json`: conexión local de desarrollo con autenticación de Windows, sin usuario ni contraseña guardados.
- `dotnet-tools.json`: manifiesto de `dotnet-ef` local, versión 10.0.12. No requiere instalación global.
- `scripts/verificar-persistencia.sql`: comprobaciones de solo lectura del estado inicial; no inserta usuarios ni ejemplos operativos.

Se usa EF Core SQL Server y Design 10.0.12, compatibles con net10.0. Se fija además System.Security.Cryptography.Pkcs 10.0.13 para corregir los avisos de la dependencia transitiva de SqlClient. Design se marca como dependencia privada de desarrollo.

Fuentes: [proveedor EF Core SQL Server 10.0.12](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.12), [herramienta local dotnet-ef](https://www.nuget.org/packages/dotnet-ef/10.0.12), [aviso de seguridad de Pkcs](https://github.com/advisories/GHSA-846f-f764-76mx).

## Conexión local

- Servidor: `(localdb)\MSSQLLocalDB`
- Base exclusiva: `BanosQuimicosDb`
- Autenticación: Windows / seguridad integrada del usuario de desarrollo.
- Instancia comprobada: SQL Server LocalDB 2019, 15.0.4382.1. No se cambió a la versión 2025 también instalada.

El fallo de arranque por nombre detectado durante autenticación quedó resuelto sin recrear la instancia. La conexión normal anterior está verificada; consultar [diagnóstico de LocalDB](localdb.md) si se repite una inconsistencia de estado.

`TrustServerCertificate=True` se utiliza solo en esta conexión local de desarrollo. La configuración de otros entornos se proporciona externamente mediante `ConnectionStrings__BanosQuimicos`; no guardar credenciales reales en el repositorio. LocalDB corresponde a la PC del backend, no al teléfono Android.

## Consultar desde Visual Studio 2026

1. Abrir `BanosQuimicos.sln`.
2. Abrir Ver > Explorador de objetos de SQL Server (SQL Server Object Explorer).
3. Agregar conexión SQL Server: servidor `(localdb)\MSSQLLocalDB`, autenticación de Windows.
4. Expandir Bases de datos > BanosQuimicosDb > Tablas.
5. En un catálogo, por ejemplo `dbo.ROLES`, usar Ver datos; también se puede abrir Nueva consulta y ejecutar el script de verificación.

## Consultar desde SQL Server Management Studio

1. Conectar a Motor de base de datos.
2. Servidor `(localdb)\MSSQLLocalDB` y autenticación de Windows, con el mismo usuario que creó la instancia.
3. Expandir Bases de datos > BanosQuimicosDb > Tablas.
4. Abrir Nueva consulta, seleccionar esa base y ejecutar, por ejemplo:

```sql
SELECT * FROM dbo.ROLES;
SELECT * FROM dbo.TIPOS_OPERACION;
SELECT * FROM dbo.ESTADOS_OPERACION;
SELECT * FROM dbo.__EFMigrationsHistory;
```

Si SSMS no está instalado, Visual Studio o `sqlcmd` son suficientes; no se instaló SSMS en esta tarea.

## Migraciones y verificación desde terminal

Ejecutar desde la raíz del repositorio. Restaurar la herramienta local y los paquetes no crea tablas:

```powershell
dotnet tool restore
dotnet restore BanosQuimicos.Api
dotnet build BanosQuimicos.sln
```

Para revisar modelo y migraciones:

```powershell
dotnet ef migrations list --project BanosQuimicos.Api -- --environment Development
dotnet ef migrations has-pending-model-changes --project BanosQuimicos.Api -- --environment Development
```

La migración inicial ya fue aplicada. Para reproducir la preparación en otra PC, primero revisar si la base existe y qué contiene; no borrar ni reinicializar una base existente. Solo después de esa revisión:

```powershell
SqlLocalDB start MSSQLLocalDB
dotnet ef database update --project BanosQuimicos.Api -- --environment Development
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d BanosQuimicosDb -b -f 65001 -i scripts/verificar-persistencia.sql
```

El script comprueba el estado inicial (una migración y tablas operativas vacías). Cuando se agreguen datos o migraciones nuevas deberá adaptarse esa expectativa; un fallo por nuevos datos no significa necesariamente corrupción.

## Las 12 tablas y el historial de EF

Las tablas de negocio son USUARIOS, ROLES, CLIENTES, BANOS_QUIMICOS, TIPOS_BANO, ESTADOS_BANO, SERVICIOS, UBICACIONES_SERVICIO, SOLICITUDES_CLIENTE, TAREAS_OPERATIVAS, TIPOS_OPERACION y ESTADOS_OPERACION.

SQL Server muestra además `__EFMigrationsHistory`: es la tabla técnica donde EF registra el identificador y versión de cada migración aplicada. Por eso se ven 13 tablas, pero siguen siendo 12 entidades del negocio.

Catálogos iniciales: 3 roles, 2 tipos de baño, 2 condiciones físicas, 11 tipos de operación y 9 estados de operación: 27 filas. No se crearon usuarios, clientes, baños ni operaciones de ejemplo. Los IDs de los catálogos son estables y las restricciones que usan esos IDs se deben actualizar con migraciones si cambia su significado.

## Qué protege la base y qué falta implementar

Ya protege:

- Email obligatorio, sin espacios exteriores, único con collation que no distingue mayúsculas/minúsculas. La propiedad Email también recorta espacios al asignarla desde C#.
- Código de baño único; un perfil CLIENTES por IdUsuario; claves foráneas y longitudes declaradas. CUIT no es único y teléfono/CUIT son texto.
- Una asignación por baño entre servicios Pendiente de entrega o Entregado, con índice único filtrado. Finalizados/cancelados no ocupan ese índice.
- Como máximo una ubicación EsActual por servicio. Consistencia básica de intervalos y fechas de los estados de servicio.
- Valores de uso, modalidad y estado de cuenta; cantidades no negativas/positivas cuando corresponda; mantenimiento como único tipo sin servicio o con bloqueo; empleado no nulo para tareas En curso/Realizada.
- Las 21 claves foráneas se crean con NO ACTION: borrar directamente un registro referenciado no arrastra el historial. Los futuros flujos de baja deben ser lógicos y no desvincular referencias históricas.

Pendiente para servicios de negocio y endpoints posteriores (no queda implementado con una FK):

- Validar ámbito de estado/tipo en solicitudes y tareas. Las FKs solo garantizan que el catálogo exista; el campo Ambito y los indicadores no validan automáticamente su uso desde otra tabla.
- Validar cliente compartido entre solicitud/servicio, baño compartido entre tarea/servicio y pertenencia de las ubicaciones al servicio.
- Registro atómico de usuario/perfil, activación y políticas de cuenta/rol ya implementados en el bloque de autenticación. Pendientes: empleado activo al asignar/avanzar y conservación de responsable/ubicación al realizar tareas del negocio.
- Disponibilidad completa según condición y mantenimiento, comprobada transaccionalmente al asignar. El índice evita duplicar asignaciones, pero no impide por sí solo asignar una unidad No apta o con mantenimiento bloqueante.
- Datos condicionales de cada solicitud (dirección de prestación, cantidad/tipo/fecha, servicio de referencia o modalidad), transición de estados y resolución por todos los trabajos requeridos.
- Cambios de dirección, intervalos históricos sin superposición y actualización coordinada de servicio, tareas y ubicaciones. Un índice de ubicación actual no comprueba todos los intervalos históricos.
- Cambios de condición por administrador, bajas lógicas y asignación/programación manual de tareas; sin generación automática semanal.

El bloque de persistencia no escribió pruebas de altas/modificaciones con datos operativos. La verificación de su script consulta estructura y catálogos reales sin insertar usuarios. El bloque posterior de autenticación sí agrega integración de cuentas en bases nuevas de prueba, según docs/autenticacion-api.md, sin alterar BanosQuimicosDb.
