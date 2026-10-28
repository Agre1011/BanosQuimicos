# Autenticación de la API

Este bloque agrega registro, login, consulta de identidad y activación administrativa. Conserva las 12 tablas y la migración inicial. No incorpora pantallas, CRUD del negocio, recuperación de contraseña, proveedores externos ni refresh tokens.

## Elección y recorrido del código

Se utiliza JWT Bearer con el paquete oficial Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12 y `PasswordHasher<Usuario>` de ASP.NET Core. Se usa únicamente el servicio estándar de hashing de Identity, sin crear sus tablas ni reemplazar el modelo aprobado.

El flujo es: contrato validado -> controlador -> EF Core/SQL Server -> respuesta limitada. Login verifica el hash y emite un JWT firmado con HMAC-SHA256. El token contiene identificador y jti, no contraseña, hash, correo ni permisos persistidos. Vigencia predeterminada: 30 minutos, tolerancia de reloj: 15 segundos. No hay renovación automática: al vencer se inicia sesión otra vez.

En cada petición autenticada se verifica firma, emisor, audiencia y vencimiento. Luego se consulta el usuario y su rol actuales en la base. Se rechazan cuentas inexistentes, Suspendidas o BajaLogica; una cuenta Pendiente no recibe rol operativo. Los claims de autorización se construyen con los datos actuales, no con un rol/estado antiguo del token. La activación administrativa se refleja en la próxima petición incluso con el token obtenido mientras estaba pendiente.

Archivos:

- `Contratos/Autenticacion/`: solicitudes con validación y respuestas sin entidades EF ni hashes.
- `Controllers/AutenticacionController.cs`: registro, login y consulta del usuario.
- `Controllers/CuentasController.cs`: activación condicional de clientes pendientes.
- `Seguridad/ConfiguracionSeguridad.cs`: autenticación, revisión de cuenta y políticas.
- `Seguridad/EmisorTokens.cs`, `OpcionesJwt.cs` y `Politicas.cs`: emisión/configuración y nombres de políticas.
- `Seguridad/PrimerAdministrador.cs`: alta local controlada del primer administrador.
- `Program.cs`: registra los servicios y ejecuta autenticación antes de autorización.
- `BanosQuimicos.Api.Pruebas/`: integración de la API real con SQL Server y JWT reales.

Referencias: [JWT Bearer de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0), [PasswordHasher](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0). Es autenticación propia de este sistema cerrado de la materia; no se presenta como servidor OAuth/OIDC.

## Configuración local sin secretos en Git

Desde la raíz del repositorio, ejecutar en PowerShell:

```powershell
./scripts/configurar-jwt-local.ps1
```

El script genera 64 bytes aleatorios y guarda `Jwt:ClaveBase64` con User Secrets, por stdin. No muestra el valor, no lo pasa como argumento y no lo escribe en el proyecto. Si ya existe una clave, la conserva. `UserSecretsId` en el csproj es solo el identificador público del almacén local. User Secrets es para desarrollo local, no un almacén de producción cifrado.

`appsettings.json` contiene únicamente emisor, audiencia y duración, sin clave. En otro entorno se debe proporcionar `Jwt__ClaveBase64` desde configuración externa. La aplicación rechaza el arranque normal si la clave falta o no contiene al menos 32 bytes. No copiar secretos, respuestas de login ni tokens a archivos `.http`, capturas o commits.

Iniciar con el perfil HTTPS de Visual Studio o desde terminal:

```powershell
dotnet run --project BanosQuimicos.Api --launch-profile https
```

Dirección HTTPS local: `https://localhost:7292`. HTTP `http://localhost:5148` se conserva para pruebas estrictamente locales. Fuera de Development se activa redirección HTTPS. No enviar contraseñas ni tokens por HTTP en una red.

## Crear el primer administrador local

La base debe tener la migración aplicada y los catálogos. Ejecutar desde una terminal interactiva, sin el servidor en depuración:

```powershell
dotnet run --project BanosQuimicos.Api --launch-profile http -- --crear-admin-local
```

El comando solo funciona en Development, pide nombre, apellido, correo, teléfono y contraseña con confirmación. La contraseña se lee sin mostrarla; no se recibe por argumentos ni archivo. Debe tener de 12 a 128 caracteres. No guardar el comando con credenciales en un script.

Se crea un usuario Administrador/Activa con hash estándar, sin perfil cliente ficticio. La operación es transaccional con aislamiento Serializable. Si existe cualquier administrador, incluso suspendido o dado de baja, se rechaza crear otro por esta vía. Tampoco convierte en administrador a un correo ya registrado. No se publican endpoints de bootstrap ni se ejecuta el alta automáticamente al iniciar la API. Al finalizar, el proceso termina sin iniciar el servidor web.

En este bloque se probó la lógica del primer administrador en bases aisladas de prueba; no se creó un administrador en BanosQuimicosDb. El ingreso interactivo de credenciales reales corresponde al desarrollador local.

## Endpoints y respuestas

| Método y ruta | Permiso | Resultado principal |
| --- | --- | --- |
| GET `/api/estado` | Público | 200, mensaje de funcionamiento. |
| POST `/api/autenticacion/registro` | Público | 201, estado Pendiente y aviso; 409 si correo duplicado; 400 si faltan datos o se envían campos no admitidos. |
| POST `/api/autenticacion/login` | Público | 200, accessToken, expiraEn, estadoCuenta y aviso si está Pendiente. Credenciales inválidas: 401; Suspendida/BajaLogica con contraseña válida: 403, sin token. |
| GET `/api/autenticacion/yo` | Cuenta identificada Activa/Pendiente | Pendiente: solo estado y aviso. Activa: id, nombre, apellido, correo, teléfono, rol y estado. Sin hashes ni contraseña. |
| POST `/api/cuentas/{id}/activar` | Administrador activo | 204 al activar Cliente/Pendiente con perfil; 404 si no existe; 409 si no cumple condiciones. |

Sin token o con token inválido: 401. Cuenta pendiente o rol incorrecto sobre una operación protegida: 403. Un token de una cuenta posteriormente suspendida/dada de baja se rechaza con 401 al consultar su estado actual.

### Registro

Campos obligatorios: `nombre`, `apellido`, `email`, `telefono`, `contrasena`. Se recortan espacios exteriores en datos de identificación; no se recorta la contraseña. Correo único sin distinguir mayúsculas, con índice de SQL Server como protección final ante concurrencia. Política técnica de contraseña elegida: 12 a 128 caracteres, sin reglas adicionales de símbolos; se aplica al registro y al bootstrap.

Campos opcionales: `razonSocial`, `cuit`, `direccionPrincipal`. Teléfono/CUIT son texto; CUIT no es único. Se rechazan propiedades extra como `idRol`, `rol`, `estadoCuenta` o `activo`. Usuario y perfil cliente se guardan en una misma transacción; un fallo revierte ambos.

### Políticas para futuros controladores

- `CuentaActiva`: se aplica por defecto con `[Authorize]` y como fallback cuando un endpoint no tiene política explícita. Pendientes no tienen acceso operativo.
- `Administrador`: exige cuenta activa y rol Administrador actual de la base.
- `CuentaIdentificada`: excepción limitada al endpoint `/yo`, que permite el aviso de espera. No usarla en servicios o solicitudes.
- `[AllowAnonymous]`: solo en estado público, registro y login.

Los controladores del negocio todavía no existen. Las pruebas cargan rutas auxiliares **solo dentro del host de pruebas** para comprobar CuentaActiva, fallback y rol Empleado; no se incorporan a la API ejecutable.

## Prueba manual reproducible sin contraseñas fijas

Con la API en HTTPS, ejecutar en una terminal PowerShell:

```powershell
$baseUrl = 'https://localhost:7292'
$correoPrueba = Read-Host 'Correo de prueba identificable y que no exista'
$claveSegura = Read-Host 'Contraseña de prueba, mínimo 12 caracteres' -AsSecureString
$clave = [System.Net.NetworkCredential]::new('', $claveSegura).Password
$registro = @{
    nombre = 'PRUEBA MANUAL'; apellido = 'Autenticacion'
    email = $correoPrueba; telefono = '+00 PRUEBA'
    contrasena = $clave
}
Invoke-RestMethod "$baseUrl/api/autenticacion/registro" -Method Post `
    -ContentType 'application/json' -Body ($registro | ConvertTo-Json)
$login = @{ email = $correoPrueba; contrasena = $clave }
$sesion = Invoke-RestMethod "$baseUrl/api/autenticacion/login" -Method Post `
    -ContentType 'application/json' -Body ($login | ConvertTo-Json)
$cabeceras = @{ Authorization = "Bearer $($sesion.accessToken)" }
Invoke-RestMethod "$baseUrl/api/autenticacion/yo" -Headers $cabeceras
```

Se debe recibir únicamente Pendiente y el aviso de espera. Repetir el registro produce 409. Usar una contraseña distinta en login produce 401. No imprimir `$sesion`, pues contiene el token.

Para activar: iniciar sesión con el primer administrador, guardar su respuesta en una variable y construir sus cabeceras como arriba. Consultar el IdUsuario del correo de prueba desde Visual Studio/SSMS (solo columnas IdUsuario, Email y EstadoCuenta), sin exponer hashes. Con ese ID:

```powershell
# $cabecerasAdmin contiene el token del administrador obtenido mediante login.
$idCliente = [int](Read-Host 'IdUsuario del cliente pendiente de prueba')
Invoke-RestMethod "$baseUrl/api/cuentas/$idCliente/activar" -Method Post -Headers $cabecerasAdmin
Invoke-RestMethod "$baseUrl/api/autenticacion/yo" -Headers $cabeceras
```

La activación devuelve 204 y `/yo` refleja la cuenta Activa. La misma activación con las cabeceras del cliente devuelve 403. No hay endpoints nuevos para suspender, dar de baja o cambiar roles; las pruebas automatizadas verifican esos escenarios modificando exclusivamente sus propias cuentas aisladas.

## Pruebas automatizadas

```powershell
dotnet test BanosQuimicos.Api.Pruebas --logger 'console;verbosity=normal'
```

Requieren LocalDB disponible para el usuario de desarrollo. Cada ejecución crea una base nueva `BanosQuimicosAuthPruebas_<GUID>` y verifica antes que no exista. Usa la migración original, la API real con TestServer y el proveedor SQL Server real; no usa EF InMemory ni sustituye el mecanismo JWT.

Las cuentas tienen nombre PRUEBA AUTENTICACION y correos `prueba-...@example.invalid`. Contraseñas y clave JWT son aleatorias en memoria; no quedan en código ni en resultados. Se conservan las bases de prueba para inspección, sin `EnsureDeleted` ni limpieza de datos existentes. No se usa BanosQuimicosDb.

Cobertura: registro/perfil/hash, normalización, duplicados concurrentes, rechazo de datos faltantes y escalamiento de privilegios, rollback ante fallo antes del commit, contraseña incorrecta, pendientes, activación exclusiva de administrador, 401/403, roles actuales, suspensión/baja con token previo, tokens alterados/vencidos/emisor-audiencia incorrectos y protección del primer administrador.

El incidente inicial de LocalDB quedó resuelto mediante parada normal del proceso desincronizado y arranque por nombre, sin borrar ni recrear la instancia/base. La API y las 12 pruebas se verificaron con `(localdb)\MSSQLLocalDB`, sin canal alternativo ni sobrescrituras de conexión. Se retiró la alternativa de servidor de la infraestructura de pruebas. Ver [diagnóstico y recuperación reproducible](localdb.md).

Para realizar personalmente el flujo de administrador y cliente en Postman, seguir la [guía paso a paso](guia-postman-autenticacion.md), que usa las rutas/cuerpos reales y distingue resultados esperados de verificaciones ya ejecutadas.
