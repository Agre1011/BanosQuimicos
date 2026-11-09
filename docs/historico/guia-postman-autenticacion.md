> Documento histórico archivado el 08/11/2026. Conserva decisiones, resultados y pendientes de su etapa; no es una guía vigente. Las actualizaciones posteriores pueden sustituir sus reglas. Consultar el [plan actual](../plan-trabajo.md) y el [índice histórico](indice.md).

# Guía manual: primer administrador, Visual Studio y Postman

**Regla vigente desde estado-cuentas (08/11/2026):** los registros nuevos son **Cliente/Activa** y no requieren aprobación. La espera y activación documentadas en evidencias históricas corresponden solo a cuentas antiguas Pendientes. Suspensión, reactivación, baja definitiva desde la app y bloqueos se describen en [la guía actual](estado-cuentas.md). No activar masivamente cuentas existentes.


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

## 4. Registrar cliente activo

POST `{{base_url}}/api/autenticacion/registro`, No Auth:

```json
{
  "nombre": "PRUEBA POSTMAN",
  "apellido": "Cliente",
  "email": "{{correo_cliente}}",
  "telefono": "+00 123 PRUEBA",
  "contrasena": "{{vault:clave_cliente}}"
}
```

Usar contraseña propia en Postman Vault (12 a 128 caracteres); no guardarla en la colección exportada. Razón social, CUIT y dirección principal pueden omitirse. No enviar rol ni estado. Esperado **201**, estadoCuenta Activa y mensaje «Cuenta creada. Ya podés iniciar sesión.». Repetir correo:409; campos obligatorios ausentes o rol/estado extra:400.

## 5. Login inmediato y consulta /yo

POST `{{base_url}}/api/autenticacion/login`, No Auth, email con la variable correo_cliente y contrasena con Vault. Esperado **200**, estadoCuenta Activa y accessToken. Guardar el token solo localmente como token_cliente. GET `{{base_url}}/api/autenticacion/yo` con Bearer token_cliente devuelve datos reales, rol Cliente y Activa, sin hashes ni contraseñas. No requiere activación administrativa. GET `/api/servicios-cliente/mios` devuelve sus servicios o lista vacía.

## 6. Activación solo de cuentas antiguas

No ejecutar este paso sobre el registro nuevo anterior. Si se dispone de una cuenta antigua Pendiente de prueba, consultar su IdUsuario en Gestionar cuentas y usar POST `{{base_url}}/api/cuentas/{{id_cliente_usuario}}/activar`, sin cuerpo, con token_admin. Admite Cliente o Empleado Pendiente con perfil:204. Repetir:409. /yo con la sesión previa de esa cuenta debe reflejar Activa y su rol real. No cambiar cuentas a Pendiente por SQL ni activar masivamente para seguir esta guía.

Para suspensión, reactivación y baja, seguir [la guía vigente](estado-cuentas.md), con los cuerpos reales y resultados esperados.

## 7. Comprobar que el cliente no activa otras cuentas

1. Registrar un segundo cliente con otro correo único, por ejemplo prueba.postman.02@example.invalid, identificándolo como PRUEBA POSTMAN. No necesita activación para preparar el intento.
2. Consultar su IdUsuario con la misma consulta SQL de solo lectura y guardarlo como `id_otro_cliente_usuario`.
3. Enviar POST `{{base_url}}/api/cuentas/{{id_otro_cliente_usuario}}/activar`, Body = none, **con token_cliente del primer cliente activo**.
4. Esperado: **403 Forbidden**, aunque el primer cliente esté Activa. La segunda cuenta permanece Activa, como nació desde el registro.
5. Repetir sin ningún token devuelve 401. El administrador tampoco necesita activar ese registro nuevo.

## Registro de resultados

Anotar métodos, rutas y códigos HTTP obtenidos, sin guardar contraseñas/tokens. Las credenciales reales del administrador se crean personalmente. La ejecución automatizada del agente usó bases aisladas; esta guía no afirma que el estudiante ya haya realizado estos pasos manuales.
