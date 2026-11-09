# Autenticación y sesión

Reglas de registro/estado/rol en [alcance](alcance-y-reglas.md); preparación y primer administrador en [ejecución local](ejecucion-local.md). Esta referencia describe el mecanismo vigente; los resultados de cada etapa están en [verificaciones](verificaciones.md).

## Implementación

JWT Bearer y `PasswordHasher<Usuario>` estándar de ASP.NET Core. No incorpora tablas Identity, OAuth/OIDC, proveedores externos, refresh tokens ni recuperación de contraseña. Hashes no se devuelven; contraseña no se guarda en claro.

Token firmado HMAC-SHA256, identificador/jti, vigencia predeterminada 30 minutos y tolerancia 15 segundos. Se verifica firma, emisor, audiencia y vencimiento. Después se consulta cuenta/rol actuales en SQL por cada petición: token anterior no permite operar durante suspensión/baja ni conserva permisos de un rol anterior. Antiguas Pendientes solo acceden a información mínima de espera. Reactivar no agrega revocación permanente de tokens antiguos todavía válidos; no se afirma una lista de revocación inexistente.

La app guarda token en SecureStorage de MAUI, nunca contraseña. URL asociada no sensible en Preferences; cambiar servidor descarta sesión anterior. Envía Bearer únicamente a su API configurada, sin seguir redirecciones ni desactivar validación HTTPS. El código compartido se prueba con almacén simulado; eso no prueba por sí solo SecureStorage nativo.

## Rutas básicas

- POST `/api/autenticacion/registro`: público; 201 Cliente/Activa, sin token. Usuario/perfil en una transacción. Correo duplicado 409, validación/campos extra 400.
- POST `/api/autenticacion/login`: público; correo/contraseña. Correcto 200 con `accessToken`, `expiraEn`, `estadoCuenta` y aviso si Pendiente. Incorrecto 401; Suspendida/BajaLogica con contraseña válida 403 sin token.
- GET `/api/autenticacion/yo`: activa devuelve identidad/contacto/rol/estado, sin secretos; Pendiente solo estado/aviso.
- POST `/api/cuentas/{id}/activar`: administrador activo; antiguas Pendientes Cliente/Empleado con perfil, 204. No forma parte del registro nuevo.

Sin token/inválido/vencido: 401. Cuenta Pendiente o rol sin permiso: 403. Token de cuenta suspendida/baja se rechaza al comprobar estado actual. La app no debe tratar 403 o fallo de conexión como resultado vacío.

Políticas en `Seguridad/ConfiguracionSeguridad.cs` y `Politicas.cs`; registro/login en `Controllers/AutenticacionController.cs`; emisión en `Seguridad/EmisorTokens.cs`; bootstrap en `Seguridad/PrimerAdministrador.cs`. CuentaActiva es protección predeterminada, CuentaIdentificada es excepción limitada a `/yo`; administrador/empleado/cliente requieren rol actual.

## Configuración sin secretos versionados

`Jwt:ClaveBase64` externa de al menos 32 bytes, más emisor/audiencia/duración. Desarrollo usa el script y User Secrets descritos en ejecución local. Producción usa configuración externa del hosting, no User Secrets del desarrollador ni claves en app/publicación versionada. Ver [publicación](publicacion.md).

Contraseña: 12 a 128 caracteres, sin recortar; correo/identificación recortados. No exportar respuestas de login, Vault, secretos o colecciones con credenciales. Prueba API reproducible: [guía Postman](guia-postman-autenticacion.md).
