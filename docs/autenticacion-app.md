# Autenticación de la app: guía de prueba

Bloque de .NET MAUI Blazor Hybrid para Windows y Android físico por USB. Usa la API existente y sus 12 tablas; no agrega CRUD ni pantallas administrativas. Login y registro toman como referencia la captura de Figma del PDF aprobado: paleta marrón, acciones azul/rosa y campos acordados. El encabezado BQ es textual; no sustituye un logotipo original. Se aplica el cambio confirmado de «Nombre de usuario» a «Correo electrónico».

## 1. Preparar la API y Windows

1. Abrir `BanosQuimicos.sln` en Visual Studio 2026, configuración **Debug**. La base y el administrador ya existentes se conservan. Si el equipo es nuevo, seguir primero [autenticación de la API](autenticacion-api.md); no crear otro administrador ni regenerar claves sin necesidad.
2. Mantener la API ejecutándose mientras se usa la app. Se puede iniciar desde otra terminal de Visual Studio, en la raíz:

   ```powershell
   dotnet run --project BanosQuimicos.Api --launch-profile https
   ```

3. El perfil `https` escucha en `https://localhost:7292` y `http://localhost:5148`. Comprobar `https://localhost:7292/api/estado`: debe devolver 200 y el mensaje de funcionamiento. La conexión SQL normal es `(localdb)\MSSQLLocalDB`, base `BanosQuimicosDb`.
4. Windows usa `https://localhost:7292/`. El certificado local de ASP.NET Core debe estar vigente y ser de confianza en Windows. Si Visual Studio pide confiar en el certificado de desarrollo, revisar y aceptar ese certificado local. Ante un error, comprobar con `dotnet dev-certs https --check`; para establecer confianza local, ejecutar personalmente `dotnet dev-certs https --trust` y revisar el aviso. No desactivar validaciones de certificados en código ni en Postman.
5. Establecer `BanosQuimicos.App` como proyecto de inicio, seleccionar **Windows Machine** y ejecutar con F5. Aparece el formulario de correo y contraseña. La API iniciada en la terminal debe seguir abierta.

También se pueden configurar ambos proyectos como inicio múltiple, verificando el perfil HTTPS de la API. No es necesario detener la API para iniciar la app.

## 2. Preparar el teléfono Android físico por USB

1. Usar el teléfono ya elegido, con depuración USB habilitada y autorizado en esta PC. No se necesita abrir puertos en la red local ni cambiar el firewall.
2. Mantener el perfil `https` anterior (también abre HTTP) o iniciar solamente la API con `--launch-profile http`. Para Android Debug debe estar disponible `http://localhost:5148/api/estado` en la PC.
3. En PowerShell de Visual Studio, comprobar el dispositivo y crear el túnel USB. Si `adb` no está en PATH, usar la ruta del SDK instalado:

   ```powershell
   $adbBanos = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
   & $adbBanos devices -l
   & $adbBanos reverse tcp:5148 tcp:5148
   & $adbBanos reverse --list
   ```

   Debe aparecer un dispositivo con estado `device`, y el mapeo del puerto 5148. Si hay varios dispositivos, agregar `-s SERIAL_DEL_TELEFONO` antes de `reverse`. Si aparece `unauthorized`, autorizar desde el teléfono. Una lista vacía requiere revisar cable, conexión y selección del dispositivo. Repetir el mapeo después de reconectar el USB o reiniciar ADB/teléfono.
4. En Visual Studio seleccionar **Debug**, el destino Android y el teléfono físico. Ejecutar la app. En Android Debug el cliente usa `http://127.0.0.1:5148/`: ese loopback del teléfono llega a la PC mediante `adb reverse`.
5. La excepción de tráfico HTTP está en el manifiesto y recurso **exclusivos de Android Debug**, limitada al dominio literal `127.0.0.1`; el resto del tráfico en claro se rechaza. No hay excepción para certificados ni permiso HTTP global. El cliente no sigue redirecciones.

`ConexionApi.cs` contiene las direcciones de desarrollo. Release conserva HTTPS y excluye la excepción y el recurso de Debug. **Antes de distribuir** hay que configurar la URL HTTPS real de la API y un certificado válido; `localhost:7292` es una dirección de desarrollo y no un servidor de producción accesible desde el teléfono. Este bloque no prepara publicación.

## 3. Registrar un cliente desde la app

1. Pulsar **Crear cuenta**. Probar primero enviar el formulario vacío: debe marcar campos obligatorios sin realizar el registro.
2. Completar nombre y apellido identificables como `PRUEBA APP`, un correo nuevo, teléfono como texto (por ejemplo con prefijo `+`) y una contraseña propia de 12 a 128 caracteres. No compartirla en el chat ni guardarla en archivos de prueba versionados. Razón social, CUIT y dirección principal son opcionales.
3. Pulsar **Crear cuenta** una sola vez. Mientras se procesa, los controles quedan deshabilitados. La API recibe `POST /api/autenticacion/registro` con los ocho campos del contrato, sin rol ni estado.
4. Esperado: aviso de cuenta creada y activación requerida; vuelve al login con el correo precargado. El registro no inicia automáticamente una sesión. Usuario y perfil se crean juntos como Cliente/Pendiente.
5. Repetir con el mismo correo debe mostrar **El correo electrónico ya está registrado** (409). Espacios exteriores y mayúsculas no permiten duplicarlo. No borrar cuentas anteriores para repetir pruebas; usar otro correo de prueba.

## 4. Ingresar como pendiente

1. Usar correo y contraseña del registro. El login real es `POST /api/autenticacion/login` con `{ "email": "correo elegido", "contrasena": "contraseña elegida" }`.
2. Después del login, la app consulta `GET /api/autenticacion/yo` con Bearer. Debe mostrar únicamente **Cuenta pendiente**, aviso, **Consultar nuevamente el estado** y **Cerrar sesión**. No debe mostrar nombre, rol ni opciones operativas.
3. Consultar nuevamente antes de activar mantiene el aviso. Una contraseña incorrecta muestra un mensaje genérico de credenciales incorrectas (401), sin indicar si el correo existe.

## 5. Activar desde Postman y comprobar acceso activo

1. Seguir [la guía de Postman](guia-postman-autenticacion.md) para iniciar sesión con el administrador existente. El cuerpo real de login contiene solo `email` y `contrasena`. Guardar su token localmente en Postman Vault, sin pegarlo en el chat.
2. Obtener el **IdUsuario** del cliente de prueba mediante esta consulta de solo lectura, reemplazando el correo:

   ```sql
   SELECT u.IdUsuario, u.Email, u.EstadoCuenta, r.NombreRol
   FROM dbo.USUARIOS u
   JOIN dbo.ROLES r ON r.IdRol = u.IdRol
   WHERE u.Email = N'correo de prueba utilizado';
   ```

3. Enviar `POST https://localhost:7292/api/cuentas/ID_USUARIO/activar`, con Bearer del administrador y **sin cuerpo**. Esperado: **204**. El cliente no dispone de esta opción en la app.
4. En la app pendiente, pulsar **Consultar nuevamente el estado**. Sin volver a iniciar sesión, si el token sigue vigente, debe aparecer **Hola, nombre** y **Rol: Cliente**. Es la pantalla mínima de una cuenta activa; no ofrece servicios ficticios ni administración.
5. Opcionalmente comprobar `/api/autenticacion/yo` desde Postman con un token de ese cliente: **200**, datos propios, rol Cliente y estadoCuenta Activa. Los tokens de Postman y de cada instalación son sesiones independientes.
6. Como en el bloque anterior, un cliente que intenta activar cuentas recibe **403**; sin token se recibe **401**. Las rutas reales y ejemplos completos están en la guía enlazada.

## 6. Sesión, desconexión y cierre

1. Cerrar y volver a abrir la app antes de vencer el token: restaura solo el token mediante **SecureStorage de MAUI** y consulta `/yo` antes de mostrar identidad. No guarda contraseñas, perfiles ni roles en almacenamiento persistente. Un indicador no sensible en Preferences permite deshabilitar la restauración al cerrar sesión.
2. Volver desde segundo plano o pulsar actualizar vuelve a consultar `/yo`. La API comprueba estado y rol actuales por petición. No se confía en el estado antiguo del login ni en decodificar el JWT en la app.
3. Detener temporalmente la API y actualizar: debe ocultarse la identidad activa y mostrarse un mensaje de conexión, con reintento o cierre de sesión. Al reiniciar la API, reintentar recupera el estado. Un tiempo de espera agotado informa que se puede reintentar. No se reenvían automáticamente registros cuyo resultado sea incierto: si el servidor los completó, el reintento puede informar correo duplicado.
4. Un **401 de `/yo`** elimina la sesión y pide ingresar otra vez: puede deberse a vencimiento, token inválido o cuenta suspendida/dada de baja. No se afirma cuál fue la causa, porque la API no la distingue en esa respuesta. Login de una cuenta suspendida/de baja devuelve **403** y muestra cuenta no habilitada. Un **403 de una operación autenticada** significa falta de permiso y no se presenta como contraseña incorrecta ni se habilita acceso.
5. Para comprobar vencimiento real, esperar los 30 minutos configurados (más tolerancia de reloj) y actualizar; debe volver al login. No alterar el reloj, claves ni cuentas existentes para simularlo. Los escenarios controlados de suspensión/baja se cubren con datos aislados en los tests de API; no existe aún pantalla para cambiar esos estados.
6. Pulsar **Cerrar sesión**, cerrar y abrir la app: debe mostrar el login. El cierre borra el token local; no hay una lista de revocación ni cierre de sesiones de otras instalaciones. Si falla el almacén seguro se informa; una sesión que no pudo guardarse solo dura mientras la app está abierta.

## 7. Lista manual en Windows y Android

Repetir el recorrido completo en ambos dispositivos, con cuentas de prueba identificadas. Registrar resultados sin credenciales ni tokens:

- Login inicial con correo vacío, registro válido, campos requeridos y opcionales, correo duplicado y credenciales incorrectas.
- Cuenta Pendiente sin perfil ni funcionalidades; activación en Postman; actualización en la app con el mismo token y pantalla activa con nombre/rol.
- Desconexión/reconexión, restauración al reiniciar, vencimiento y cierre de sesión.
- En Android: título y botones fuera de las barras del sistema; teclado en correo/teléfono/contraseña; desplazamiento hasta dirección y ambos botones con teclado abierto, cierre del teclado, retrato y paisaje. El ajuste usa `SafeAreaEdges="All"` y `WindowSoftInputModeAdjust="Resize"`, sin márgenes superiores fijos.
- En Windows: ventana reducida, tabulación y desplazamiento hasta todos los campos. No considerar la compilación como prueba de teclado o del resultado visual Android.

## Verificación automatizada y límites

Ejecutar `dotnet test BanosQuimicos.Api.Pruebas`. El proyecto enlaza el código C# real del cliente/sesión para probarlo sin depender de un runtime MAUI. Usa un almacén simulado; no certifica el SecureStorage nativo. Las pruebas de integración ejecutan los controladores reales con TestServer y SQL Server LocalDB en una base nueva `BanosQuimicosAuthPruebas_<GUID>` que se conserva, nunca sobre BanosQuimicosDb.

Referencias técnicas: [áreas seguras MAUI 10](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/safe-area?view=net-maui-10.0), [teclado Android](https://learn.microsoft.com/en-us/dotnet/maui/android/platform-specifics/soft-keyboard-input-mode?view=net-maui-10.0), [SecureStorage](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage?view=net-maui-10.0) y [configuración de seguridad de red Android](https://developer.android.com/privacy-and-security/security-config).
