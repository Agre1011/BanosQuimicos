# Guía manual: primer administrador, Visual Studio y Postman

Estas pruebas las realiza el estudiante con sus propias contraseñas. No enviarlas al chat ni escribirlas en código, archivos versionados o colecciones exportadas. Las rutas y cuerpos corresponden a la implementación actual; no se agregan endpoints del negocio.

## 1. Crear personalmente el primer administrador

Detener la depuración de la API y abrir una terminal PowerShell interactiva en la raíz del proyecto:

```powershell
SqlLocalDB start MSSQLLocalDB
./scripts/configurar-jwt-local.ps1
dotnet run --project BanosQuimicos.Api --launch-profile http -- --crear-admin-local
```

El script conserva la clave JWT local si ya existe. El comando pide nombre, apellido, correo electrónico, teléfono y contraseña con confirmación. Escribir la contraseña solo en esa terminal: no se muestra ni se guarda en el comando. Usar entre 12 y 128 caracteres. Recordar el correo y la contraseña para el login posterior.

Resultado esperado: `Administrador local creado. No se inició el servidor web.` Se crea Administrador/Activa, sin perfil cliente ficticio. Si informa que ya existe un administrador, no crear otro ni borrar el existente; comprobar las cuentas desde SQL. Este comando no inicia una API que quede ejecutándose.

## 2. Ejecutar la API desde Visual Studio 2026

1. Abrir BanosQuimicos.sln.
2. En el Explorador de soluciones, clic derecho sobre BanosQuimicos.Api > Establecer como proyecto de inicio.
3. En el selector de perfiles junto al botón de inicio, elegir `https`.
4. Presionar F5. Aceptar la confianza del certificado HTTPS de desarrollo si Visual Studio lo solicita. Si es necesario, ejecutar personalmente `dotnet dev-certs https --trust` y volver a iniciar.
5. Mantener la API ejecutándose mientras se usa Postman. Debe escuchar en `https://localhost:7292` (y HTTP local 5148 según el perfil). No hay Swagger ni página de inicio; un 404 en `/` no indica que haya fallado.
6. Consultar `GET https://localhost:7292/api/estado` sin autenticación: 200 con `{"mensaje":"La API de Baños Químicos está funcionando."}`.

Si la conexión SQL falla, consultar [diagnóstico de LocalDB](localdb.md). No sustituir la conexión por un canal temporal. Si el certificado falla en Postman, revisar la confianza del certificado de desarrollo, sin desactivar globalmente la verificación SSL.

## Preparar Postman

Usar Postman de escritorio para las peticiones a localhost. Crear un entorno local con `base_url = https://localhost:7292` y `correo_admin` igual al correo elegido en la consola. Elegir un correo de prueba único, por ejemplo `prueba.postman.01@example.invalid`, para `correo_cliente`.

En Postman Local Vault guardar `clave_admin` y `clave_cliente`; escribir allí los valores propios, sin compartirlos. Se referencian como `{{vault:clave_admin}}` y `{{vault:clave_cliente}}`. Tras cada login guardar su accessToken en Local Vault como `token_admin` o `token_cliente`. Así los cuerpos siguientes son plantillas, no contraseñas reales guardadas en archivos.

Para peticiones JSON: Body > raw > JSON; Postman debe enviar Content-Type: application/json. Para peticiones protegidas: Authorization > Bearer Token y pegar la referencia al token, sin escribir el prefijo Bearer en el campo. Elegir Bearer Token, no el tipo JWT Bearer que genera tokens nuevos.

Referencia: [Bearer Token en Postman](https://learning.postman.com/docs/use/send-requests/authorization/authorization-types/), [uso de secretos del Local Vault](https://learning.postman.com/docs/use/postman-vault/use-vault-secrets/).

## 3. Login del administrador

Método POST, URL `{{base_url}}/api/autenticacion/login`, Authorization = No Auth. Body raw JSON:

```json
{
  "email": "{{correo_admin}}",
  "contrasena": "{{vault:clave_admin}}"
}
```

Esperado: **200 OK**, con accessToken, expiraEn, estadoCuenta = Activa y mensaje = null. Contraseña incorrecta: 401. Guardar accessToken en Local Vault como token_admin, sin subirlo al chat.

Comprobar `GET {{base_url}}/api/autenticacion/yo`, Authorization Bearer Token = `{{vault:token_admin}}`. Esperado: 200, rol Administrador y estadoCuenta Activa; el idUsuario es el del administrador. Nunca aparecen contraseña ni hash.

## 4. Registrar cliente y comprobar que queda Pendiente

POST `{{base_url}}/api/autenticacion/registro`, Authorization = No Auth:

```json
{
  "nombre": "PRUEBA POSTMAN",
  "apellido": "Cliente",
  "email": "{{correo_cliente}}",
  "telefono": "+00 123 PRUEBA",
  "contrasena": "{{vault:clave_cliente}}"
}
```

Razón social, CUIT y dirección principal pueden omitirse. No agregar rol, IdRol ni estado. La contraseña debe tener de 12 a 128 caracteres. Elegir un correo nuevo si ya existe; no borrar cuentas para repetir la prueba.

Esperado: **201 Created**:

```json
{
  "estadoCuenta": "Pendiente",
  "mensaje": "Tu cuenta está pendiente de activación por el administrador."
}
```

Repetir el mismo registro produce 409. Registrar sin teléfono, o enviando rol/estado, produce 400.

Hacer login del cliente por POST `/api/autenticacion/login`, No Auth:

```json
{
  "email": "{{correo_cliente}}",
  "contrasena": "{{vault:clave_cliente}}"
}
```

Esperado: **200**, token y estadoCuenta Pendiente con el aviso. Guardar accessToken como token_cliente. `GET /api/autenticacion/yo` con ese token debe devolver solo los dos campos del aviso anterior, sin datos de perfil ni rol.

Para obtener el ID que necesitará el administrador, abrir Visual Studio > Ver > Explorador de objetos de SQL Server, conectar a `(localdb)\MSSQLLocalDB`, elegir BanosQuimicosDb y ejecutar una consulta de solo lectura con el correo real de prueba:

```sql
SELECT u.IdUsuario, u.Email, u.EstadoCuenta, r.NombreRol, c.IdCliente
FROM dbo.USUARIOS u
JOIN dbo.ROLES r ON r.IdRol = u.IdRol
LEFT JOIN dbo.CLIENTES c ON c.IdUsuario = u.IdUsuario
WHERE u.Email = N'prueba.postman.01@example.invalid';
```

Esperado: una fila Cliente/Pendiente y un IdCliente no nulo. Guardar **IdUsuario**, no IdCliente, como variable `id_cliente_usuario` en Postman. El registro pendiente no expone un listado administrativo para obtenerlo; en este bloque se consulta desde SQL local.

Con token_cliente, POST `{{base_url}}/api/cuentas/{{id_cliente_usuario}}/activar`, Body = none, debe dar **403 Forbidden** y mantener Pendiente. La API, no solo la interfaz, rechaza la autoactivación.

No hay todavía controladores de servicios ni solicitudes. No usar un 404 de una ruta inexistente como prueba de permisos ni esperar datos de negocio en esta etapa. La restricción general CuentaActiva está verificada en las pruebas de integración con rutas auxiliares exclusivas del host de pruebas; en Postman se comprueban el aviso mínimo y la denegación de activación con rutas reales.

## 5. Activarlo como administrador

POST `{{base_url}}/api/cuentas/{{id_cliente_usuario}}/activar`, Body = none. En Authorization, usar **token_admin**.

Esperado: **204 No Content**, sin JSON de respuesta. En SQL el cliente pasa a Activa. Repetir la activación con token_admin da 409, porque ya no está Pendiente. Un ID inexistente da 404 solo si el solicitante es un administrador autorizado.

## 6. Comprobar el acceso del cliente y consultar /yo

GET `{{base_url}}/api/autenticacion/yo`, usando **token_cliente**. El token anterior puede seguir usándose si no venció: la API consulta el estado actual en cada petición.

Esperado: **200**, con estos campos y los valores de esa cuenta:

```json
{
  "idUsuario": 0,
  "nombre": "PRUEBA POSTMAN",
  "apellido": "Cliente",
  "email": "correo de prueba utilizado",
  "telefono": "+00 123 PRUEBA",
  "rol": "Cliente",
  "estadoCuenta": "Activa"
}
```

El 0 es ilustrativo: idUsuario debe ser el ID real consultado en SQL. Ya no se recibe únicamente el aviso de espera. Si el token venció (30 minutos), repetir login y reemplazar token_cliente. Sin token o con un token inválido, `/yo` da 401.

Esto verifica identidad y estado de acceso habilitado; no demuestra un CRUD de servicios que aún no existe.

## 7. Comprobar que el cliente no activa otras cuentas

1. Registrar un segundo cliente con otro correo único, por ejemplo prueba.postman.02@example.invalid, identificándolo como PRUEBA POSTMAN. No necesita activación para preparar el intento.
2. Consultar su IdUsuario con la misma consulta SQL de solo lectura y guardarlo como `id_otro_cliente_usuario`.
3. Enviar POST `{{base_url}}/api/cuentas/{{id_otro_cliente_usuario}}/activar`, Body = none, **con token_cliente del primer cliente activo**.
4. Esperado: **403 Forbidden**, aunque el primer cliente esté Activa. SQL debe mostrar que el segundo sigue Pendiente.
5. Repetir sin ningún token devuelve 401. No repetir con token_admin salvo que se desee activar realmente al segundo cliente.

## Registro de resultados

Anotar métodos, rutas y códigos HTTP obtenidos, sin guardar contraseñas/tokens. Las credenciales reales del administrador se crean personalmente. La ejecución automatizada del agente usó bases aisladas; esta guía no afirma que el estudiante ya haya realizado estos pasos manuales.
