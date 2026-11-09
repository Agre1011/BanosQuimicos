# Base de datos y persistencia

App → API → EF Core → SQL Server. La app no conecta directamente a SQL. Modelo en `BanosQuimicos.Api/Modelos`, configuraciones en `Datos/Configuraciones`, contexto en `Datos/BanosQuimicosDbContext.cs` y migraciones/snapshot en `Datos/Migraciones`. La API **no migra automáticamente al iniciar**.

## Las 12 tablas

- **ROLES:** Administrador, Empleado y Cliente; referenciado por USUARIOS.
- **USUARIOS:** identidad, correo, hash, rol, estado y datos de suspensión/baja. Empleado es un rol, no otra tabla.
- **CLIENTES:** perfil opcional de un usuario, único por IdUsuario. Registro público crea ambos juntos; no se elimina perfil al cambiar rol.
- **TIPOS_BANO:** Obra/Evento, tipo físico del inventario.
- **ESTADOS_BANO:** condición Apto/No apto, no disponibilidad ni baja lógica.
- **BANOS_QUIMICOS:** código único, tipo/condición, responsable habitual opcional, clave de alta y fecha de baja opcionales. Referencia de servicios y tareas históricas.
- **SERVICIOS:** cliente, baño, solicitud origen opcional, uso, frecuencia, estado y fechas. Un registro por baño; varios pueden provenir de una misma solicitud.
- **UBICACIONES_SERVICIO:** dirección por servicio con intervalos y EsActual; las tareas conservan referencias históricas, incluido destino de traslado.
- **TIPOS_OPERACION:** catálogo compartido con indicadores de uso para solicitud/tarea. Adicional histórico se conserva sin ofrecerlo.
- **ESTADOS_OPERACION:** catálogo con ámbito solicitud/tarea.
- **SOLICITUDES_CLIENTE:** cliente, servicio opcional, tipo/estado, datos del pedido/modalidad/destino, descripción, respuesta administrativa/fecha, ClaveEnvio y revisión técnica de ubicación opcionales.
- **TAREAS_OPERATIVAS:** baño directo, servicio/solicitud/empleado opcionales según operación, tipo/estado, fecha programada, origen/destino, bloqueo, descripción y cumplimiento. Registro de trabajo guarda autor/fecha; finalización independiente.

`__EFMigrationsHistory` es técnica: registra migración/versión EF, sin ser una entidad de negocio. Por eso se ven 13 tablas. Catálogos iniciales: 3 roles + 2 tipos + 2 condiciones + 11 operaciones + 9 estados = **27 filas**; no incluyen usuarios ni datos operativos de ejemplo.

## Restricciones y lógica

Configuraciones declaran longitudes, obligatorios/opcionales, FK sin cascada e índices: correo único normalizado sin distinguir mayúsculas, código de baño único, usuario del perfil único, una asignación vigente por baño y una ubicación actual por servicio. Claves opcionales de alta/programación/envío tienen índices únicos filtrados `IS NOT NULL`, conservando múltiples registros antiguos sin clave.

La base conserva límites y consistencia básica; no sustituye reglas de API sobre rol/estado, ámbito de catálogo, propiedad, empleado activo, baño/servicio/ubicación compatibles, disponibilidad, estados actuales y revisión de trabajos. Esas operaciones usan las transacciones/bloqueos existentes, que esta limpieza no modifica. Reglas funcionales en [alcance](alcance-y-reglas.md).

## Conectar y consultar

Desarrollo: `(localdb)\MSSQLLocalDB`, `BanosQuimicosDb`, autenticación Windows del propietario. Configuración en `appsettings.Development.json`; `TrustServerCertificate=True` limitado a esa conexión local. No usar dirección temporal de canal. Fuera de desarrollo: conexión externa, ver [publicación](publicacion.md).

Visual Studio 2026: Ver → Explorador de objetos de SQL Server → agregar servidor LocalDB con autenticación Windows → Bases de datos → BanosQuimicosDb → Tablas → Ver datos/Nueva consulta. SSMS: Motor de base de datos, mismo servidor/usuario y base. No editar estados para fabricar pruebas.

```sql
SELECT DB_NAME() AS BaseActual;
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
SELECT IdRol, NombreRol FROM dbo.ROLES;
```

## Migraciones conservadas

1. `20261027203511_Inicial`: esquema y catálogos.
2. `20261106204848_RegistroTrabajoTarea`: autor/fecha opcionales de trabajo, FK, índice y CHECK que mantiene ambos juntos.
3. `20261106222844_ResponsableLimpiezaYProgramacion`: responsable habitual opcional del baño, FK/índice; clave de programación opcional con índice único filtrado.
4. `20261107191916_ClaveEnvioSolicitud`: clave opcional de solicitud e índice único filtrado.
5. `20261107211946_RevisionUbicacionSolicitud`: `RevisionUbicacionJson` opcional, instantánea técnica para revisar tareas/destino sin reescribir historial prematuramente.
6. `20261108174906_InventarioBanosYMantenimiento`: FechaBaja y ClaveAlta opcionales en baños, índice filtrado/CHECK; DescripcionTrabajo opcional hasta 2000 en tareas.
7. `20261108193729_RegistroClienteActivo`: solo DEFAULT EstadoCuenta a Activa; no cambia estados de cuentas existentes. La API además establece explícitamente Cliente/Activa.

Aplicaciones locales y pendientes de hosting están registrados históricamente; no se consultaron bases durante esta limpieza. Lista de despliegue en [publicación](publicacion.md). No se agregan migraciones ahora.

Equipo local nuevo: revisar primero base/conexión e historial. `dotnet tool restore` usa `dotnet-tools.json` (dotnet-ef local); `dotnet ef migrations list --project BanosQuimicos.Api -- --environment Development` permite revisar historial. Solo tras comprobar destino y necesidad, `dotnet ef database update --project BanosQuimicos.Api -- --environment Development` aplica faltantes; no borrar/recrear bases.

`scripts/verificar-persistencia.sql` fue escrito para la etapa Inicial, con tablas operativas vacías; no es un control universal de una base con datos/migraciones posteriores. Los scripts `preparar-*-local.sql` son escenarios locales, no semillas de producción. Se conservan por reproducibilidad, sin ejecutarlos en esta limpieza.
