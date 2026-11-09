# Publicación: estado y procedimiento pendiente

API existente: `https://celeris.runasp.net/`, MonsterASP, base `db77593`. La versión alojada es anterior a los últimos bloques locales; que responda HTTPS no demuestra que tenga el esquema/código actual. No se contactó ni modificó hosting en esta limpieza.

Orden acordado: completar pendientes funcionales → integral Android con API local → correcciones → migraciones y republicación → comprobación HTTPS alojada. Ver [plan](plan-trabajo.md). No ejecutar estos pasos hasta autorizar el bloque de publicación.

## Verificar proveedor y configuración

- ASP.NET Core **.NET 10**, runtime/Hosting Bundle compatible y arquitectura del proceso publicado; comprobar parche soportado en panel, no asumirlo por el runtime de la PC.
- SQL Server compatible con el proveedor EF Core 10 y el SQL generado. Se ha usado LocalDB 2019 localmente; verificar versión/compatibilidad real del servidor alojado y soporte de índices filtrados, restricciones/FK, `datetimeoffset`, `uniqueidentifier`, transacciones Serializable y `sys.sp_getapplock` con propietario Transaction. Las asignaciones dependen de ese bloqueo, no solo de crear tablas. Revisar también SQL generado para consultas de colecciones y compatibilidad JSON cuando corresponda.
- Permisos para DDL/migraciones y operaciones de la API, conexión cifrada/certificado válido, backups y recuperación. Confirmar destino explícito antes de ejecutar SQL; no modificar bases de otros proyectos.
- Configuración externa: `ConnectionStrings__BanosQuimicos` y `Jwt__ClaveBase64`, además de emisor/audiencia/duración coherentes. Usar configuración privada del panel/mecanismo soportado; no Git, app MAUI, perfiles versionados ni cadenas en scripts. No registrar valores secretos. Production, HTTPS válido, sin excepciones globales de certificados.

## Migraciones

Inicial fue preparada/publicada anteriormente. El script conservado `scripts/publicacion/db77593-inicial.sql` cubre **0 → 20261027203511_Inicial**, crea 12 tablas del negocio más historial EF y carga 27 filas de catálogos, sin usuarios ni ejemplos. Es solo para una base vacía comprobada; no volver a ejecutarlo en la base existente.

Pendientes documentadas para hosting, en orden:

1. `20261106204848_RegistroTrabajoTarea`
2. `20261106222844_ResponsableLimpiezaYProgramacion`
3. `20261107191916_ClaveEnvioSolicitud`
4. `20261107211946_RevisionUbicacionSolicitud`
5. `20261108174906_InventarioBanosYMantenimiento`
6. `20261108193729_RegistroClienteActivo`

Significado de cada una en [base de datos](base-datos.md). No afirmar su ausencia definitiva sin leer `__EFMigrationsHistory` del destino al publicar.

Procedimiento futuro: respaldo y lectura de historial/tablas → comprobar modelo compilado → generar script desde última migración realmente aplicada hasta RegistroClienteActivo → revisar SQL y conteos esperados → aplicar una vez al destino confirmado → verificar historial, columnas, índices, FK y catálogos. Ejemplo de generación **si se confirma que solo está Inicial**:

```powershell
dotnet ef migrations script 20261027203511_Inicial 20261108193729_RegistroClienteActivo --project BanosQuimicos.Api -o scripts/publicacion/actualizacion-revisar.sql
```

Este ejemplo no fue ejecutado ni genera un archivo como parte de limpieza. Para una base nueva, generar desde `0` hasta el objetivo, no copiar LocalDB. No copiar usuarios, hashes, contraseñas ni datos de prueba. No ejecutar Down ni recrear bases. Las migraciones agregan estructura/default; no activan masivamente cuentas antiguas.

## Primer administrador y comprobación posterior

No crear otro si ya existe. El bootstrap `--crear-admin-local` solo funciona en Development y terminal interactiva; no es endpoint remoto. Si una base nueva realmente carece de administrador, usar proceso local controlado con conexión externa al destino comprobado y contraseña escrita personalmente, según [ejecución](ejecucion-local.md); no habilitar Development ni credenciales de bootstrap en la API pública. Retirar la sobrescritura de conexión local después. No guardar secretos en comandos/versionado.

Publicar API compatible después de migrar con ventana que evite mezclar código viejo/esquema nuevo de forma insegura. Seleccionar hosting en `ConexionApi.cs`, recompilar app y comprobar HTTPS, sesiones, roles y recorridos de negocio; token local no debe reutilizarse. No se dan por aprobadas estas comprobaciones futuras.
