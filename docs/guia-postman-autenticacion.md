# Comprobar autenticación con Postman

Preparar API HTTPS y primer administrador con [ejecución local](ejecucion-local.md). No crear otro administrador ni cambiar cuentas existentes para reproducir ejemplos. Reglas/respuestas en [autenticación](autenticacion-api.md).

## Preparación

Postman de escritorio, `base_url = https://localhost:7292`. Contraseñas y tokens solo en **Local Vault**, nunca en colección exportada, documentación o chat. Variables de correo con valores propios de prueba; ejemplos de nombre no son cuentas garantizadas. Mantener validación SSL.

Grupo 1: GET `{{base_url}}/api/estado`, sin autenticación: 200. POST `{{base_url}}/api/autenticacion/login`, No Auth, Body raw JSON:

```json
{"email":"{{correo_admin}}","contrasena":"{{vault:clave_admin}}"}
```

200 con token; guardar su valor solo en Vault como `token_admin`. GET `/api/autenticacion/yo`, Authorization Bearer Token `{{vault:token_admin}}`: identidad, Administrador/Activa, sin hash. Contraseña incorrecta devuelve 401.

Grupo 2: POST `/api/autenticacion/registro`, No Auth, JSON:

```json
{
  "nombre":"PRUEBA POSTMAN",
  "apellido":"Cliente",
  "email":"{{correo_cliente}}",
  "telefono":"+00 PRUEBA",
  "contrasena":"{{vault:clave_cliente}}"
}
```

Correo nuevo y contraseña propia de 12–128 caracteres. 201 Cliente/Activa; no enviar rol/estado. Campos opcionales pueden omitirse. Repetir correo:409; datos inválidos/propiedades extra:400. Login con ese correo y su contraseña:200; guardar token como `token_cliente`, consultar `/yo`: Cliente/Activa sin aprobación.

Grupo 3: si ya existe una cuenta **antigua Pendiente** de prueba, obtener ID desde Gestionar cuentas; POST `/api/cuentas/{id}/activar` sin cuerpo con Bearer administrador:204. No cambiar nuevos registros a Pendiente por SQL. Con token cliente/empleado:403; sin token:401. Repetición puede devolver409; la app consulta y muestra el estado actual.

Para verificar aislamiento operativo, usar las consultas propias con token cliente y no rutas administrativas. Otros recorridos se prueban desde las guías por rol. Estos son resultados esperados, no nuevas pruebas ejecutadas; evidencia en [verificaciones](verificaciones.md).
