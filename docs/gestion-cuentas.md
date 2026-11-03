# Consulta y activación de cuentas

Esta guía describe el bloque original de consulta y activación. La ampliación posterior permite cambiar Cliente ↔ Empleado sobre la cuenta existente: ver [acceso de empleados](acceso-empleados.md). No se crean empleados mediante un alta separada ni se permite editar datos, suspender o dar de baja. No agrega tablas o migraciones. Las 12 tablas del negocio y el historial de EF permanecen sin cambios.

## Referencia aprobada y adaptación

Se revisaron el texto de las páginas 1-2 del PDF aprobado y la captura Figma del administrador (página 6). Se conservan la paleta y la presentación por tarjetas de Clientes y Empleados. Algunos textos pequeños no son legibles; no se tomaron como reglas. «Gestionar cuentas», búsqueda, filtro, paginación, detalle y confirmación responden al alcance autorizado de este bloque. La baja sigue fuera de alcance; el cambio de rol se incorporó después según la captura ampliada y la corrección del estudiante, como explica la guía de empleados. Los originales se conservan.

## API: contratos reales

Todas las rutas requieren **Administrador con cuenta Activa**. La API consulta el rol/estado actuales por petición, incluso cuando el token fue emitido antes. Sin token válido: 401. Cliente activo, empleado activo o cuenta Pendiente: 403. Cuenta Suspendida/BajaLogica con token anterior: 401. Nunca basta con ocultar un botón.

### Listado

`GET /api/cuentas?pagina=1&tamanoPagina=10&busqueda=ana&estado=Pendiente`

- `pagina`: desde 1; predeterminada 1. Se rechaza un desplazamiento que exceda el límite admitido por Skip.
- `tamanoPagina`: de 1 a 50; predeterminado 10. La app usa 10.
- `busqueda`: opcional, hasta 254 caracteres; se recortan espacios exteriores y se busca un fragmento en nombre, apellido o correo. La comparación usa la intercalación de SQL Server; el correo conserva su comparación sin mayúsculas. Los caracteres como % se buscan literalmente, sin convertirlos en comodines SQL.
- `estado`: omitir para todos; valores exactos Pendiente, Activa, Suspendida y BajaLogica. Un valor inválido devuelve 400.
- Orden: apellido, nombre e IdUsuario como desempate. La consulta está paginada en SQL; no descarga todos los usuarios para filtrar en la app. Es un listado actual, no una fotografía inmutable ante altas concurrentes.
- Respuesta 200: `elementos`, `pagina`, `tamanoPagina` y `total` filtrado. Página posterior al final: elementos vacíos con el total real. Sin coincidencias: total 0 y lista vacía. Parámetros fuera de rango: 400.

Cada elemento contiene exclusivamente `idUsuario`, `nombre`, `apellido`, `email`, `telefono`, `rol`, `estadoCuenta`, `fechaRegistro` y `puedeActivar`. No devuelve entidades EF, contraseñas, hashes, tokens ni datos de configuración.

### Detalle

`GET /api/cuentas/ID_USUARIO`: 200 con los mismos campos de gestión; 404 si no existe. La fecha se guarda como DateTimeOffset y la app la muestra en hora local, con etiqueta explícita.

`puedeActivar` solo es verdadero para Cliente/Pendiente con perfil cliente existente. Este indicador orienta la interfaz; el POST vuelve a validar todas las condiciones en SQL.

### Activación existente

`POST /api/cuentas/ID_USUARIO/activar`, Bearer del administrador, **sin cuerpo**.

- 204: activación realizada.
- 409: la cuenta cambió o no cumple las condiciones; no se modifica ni se reactiva.
- 404: cuenta inexistente.

Se conserva el UPDATE condicional existente: ante dos activaciones simultáneas, una da 204 y la otra 409. La app no reintenta automáticamente el POST. Si recibe 409 informa que la cuenta cambió y vuelve a leer detalle/listado; muestra el estado actual sin un error inesperado. También actualiza ambos después de un 204. Si la última cuenta deja de pertenecer a la última página del filtro Pendiente, ajusta la página al último número válido.

## Prueba manual en Windows y Moto G9 Plus por USB

Usar [la preparación de autenticación](autenticacion-app.md): API perfil https para Windows, certificado local válido, Android Debug con `adb reverse tcp:5148 tcp:5148`. No cambiar certificados, firewall ni URL de conexión. Mantener la API ejecutándose. No publicar.

1. Registrar desde la app un cliente nuevo identificado como PRUEBA CUENTAS, con un correo único y contraseña elegida localmente. No registrar contraseñas o tokens en Git ni en el chat; no modificar cuentas personales para preparar las pruebas.
2. Entrar con el administrador existente. Esperado: nombre/rol y botón **Gestionar cuentas**. Entrar con un cliente activo o pendiente: ese acceso no aparece; Pendiente conserva únicamente su aviso y opciones de estado/cierre.
3. Pulsar Gestionar cuentas. Esperado: carga, tarjetas con nombre/apellido, correo, rol y estado; total y página. Buscar por un fragmento del nombre, del apellido y luego por correo. Esperado: coincidencias correspondientes. Elegir Pendiente: solo cuentas pendientes. Combinar búsqueda y filtro. Un texto inexistente muestra lista vacía, sin error.
4. Comprobar Anterior/Siguiente y el tamaño 10. Con menos de 11 resultados, Siguiente debe estar deshabilitado; no es un fallo. No crear grandes lotes de cuentas personales para probar: la suite aislada cubre 23 registros y cuatro páginas, incluidos nombres iguales.
5. Abrir **Ver detalle** del cliente de prueba. Esperado: identidad, correo, teléfono, rol, estado y fecha de registro; botón Activar cliente. No hay contraseñas, hashes ni tokens ni controles de edición. Volver al listado conserva los filtros aplicados y consulta otra vez.
6. Pulsar Activar cliente. Debe pedir confirmación mostrando nombre y correo. Pulsar **Cancelar**: debe permanecer Pendiente. Repetir y **Confirmar activación**: controles deshabilitados durante la petición; aviso de éxito y detalle Activa, sin botón para volver a activar. Volver al listado: estado actualizado; bajo filtro Pendiente el cliente ya no aparece.
7. Para probar la carrera, registrar otro cliente de prueba. Abrir su detalle pendiente en Windows y Android con el administrador. Activar en una instalación. Confirmar desde la otra, que todavía muestra la información anterior: debe informar cambio en otra sesión y mostrar Activa, sin repetir el alta ni fallar inesperadamente. También se puede realizar la primera activación desde Postman con el endpoint anterior.
8. Detener temporalmente la API y actualizar listado/detalle. Esperado: error de conexión y ocultamiento de identidad administrativa; reintento o cierre de sesión. Reiniciar API y reintentar: consulta /yo y recupera acceso si sigue autorizado. No conservar tarjetas sensibles visibles ante un 401/403 o una conexión no verificable.
9. En Windows comprobar ventana reducida, tabulación y desplazamiento. En Android comprobar título completo, teclado de búsqueda, filtro, desplazamiento por tarjetas/detalle y rotación. Cancelar la confirmación no envía POST. No se afirma que estas pantallas nuevas ya hayan sido probadas personalmente.
10. Volver al inicio y cerrar sesión. Comprobar que no se restaura acceso administrativo al volver a abrir. Las reglas de SecureStorage y de sesión del bloque anterior se conservan.

## Verificación de API desde Postman

Usar el login y Vault de [la guía de autenticación](guia-postman-autenticacion.md). Sustituir `ID_USUARIO` con el devuelto por el listado, sin consulta SQL ni acceso a hashes.

| Petición | Autorización | Resultado esperado |
| --- | --- | --- |
| GET /api/cuentas?pagina=1&tamanoPagina=1 | Administrador activo | 200, máximo un elemento, total filtrado |
| GET /api/cuentas?estado=Pendiente&busqueda=PRUEBA | Administrador activo | 200, solo coincidencias pendientes |
| GET /api/cuentas?tamanoPagina=51 | Administrador activo | 400 |
| GET /api/cuentas?estado=Inventado | Administrador activo | 400 |
| GET /api/cuentas/ID_USUARIO | Administrador activo | 200, campos permitidos |
| GET /api/cuentas/2147483647 | Administrador activo | 404 si no existe |
| GET listado o detalle; POST activación | Sin token | 401 |
| GET listado o detalle; POST activación | Cliente activo o Pendiente | 403 |
| POST activación de cliente pendiente, sin cuerpo | Administrador activo | 204 |
| Repetir POST con la misma cuenta ya activa | Administrador activo | 409; GET devuelve Activa |

No existe operación de suspensión. El cambio de rol ya cuenta con PUT /api/cuentas/{id}/rol según la ampliación de empleados, que se prueba exclusivamente contra API local en este bloque. Los escenarios automatizados usan datos aislados y no alteran nuestras cuentas existentes.

## Pruebas automatizadas y pendiente explícito

`dotnet test BanosQuimicos.Api.Pruebas` usa bases nuevas BanosQuimicosAuthPruebas_<GUID>, SQL Server LocalDB y TestServer. No elimina bases ni registros anteriores, y no usa BanosQuimicosDb. Los datos se identifican como PRUEBA CUENTAS/example.invalid y las claves se generan en memoria. El plan registra resultados reales de compilación/tests; no equivalen a pruebas visuales ni de interacción nativa.

**El vencimiento real del token sigue pendiente.** Los rechazos automatizados por 401 y los tokens artificialmente vencidos no sustituyen esperar el vencimiento real en la app. La compilación tampoco verifica los controles visuales en el teléfono.
