> Documento histórico archivado el 08/11/2026. Conserva decisiones, resultados y pendientes de su etapa; no es una guía vigente. Las actualizaciones posteriores pueden sustituir sus reglas. Consultar el [plan actual](../plan-trabajo.md) y el [índice histórico](indice.md).

# Registro activo y estado de cuentas

Bloque `feature/estado-cuentas`, API local y LocalDB. Esta decisión confirmada reemplaza el registro Cliente/Pendiente para **cuentas nuevas**. Las evidencias antiguas se conservan como historia; no son instrucciones de activar cada registro nuevo.

## Reglas vigentes

- El registro crea usuario y perfil de cliente juntos, con **Cliente/Activa** fijados explícitamente por la API. Rechaza campos extra de rol/estado. Mantiene validaciones, correo único normalizado y hash estándar. No inicia sesión automáticamente: informa que puede ingresar y vuelve al login habitual con el correo completado.
- Las cuentas antiguas Pendientes conservan su restricción hasta que un administrador las active. La activación acepta tanto Cliente como Empleado con perfil. No hay activación masiva ni conversión de estados por la migración.
- El rol y el estado se administran por separado. Cliente ↔ Empleado no activa ni reactiva. El administrador no puede asignar el rol Administrador mediante este selector.
- Suspender exige motivo no vacío, de hasta 1000 caracteres. Reactivar solo permite Suspendida → Activa y conserva el rol y último motivo de suspensión. La baja lógica es **definitiva desde la app** en esta versión: no existe acción para restaurarla; conserva usuario, perfil, contraseña, servicios, solicitudes e historial y registra FechaBaja.
- No se permite cambiar el propio estado administrativo ni dejar al sistema sin administradores activos. La API comprueba otra vez al administrador dentro de la transacción y protege las operaciones simultáneas.
- Para suspender o dar de baja se comprueban tareas del usuario y operaciones de su perfil de cliente, incluso en datos antiguos que mantengan ambos tipos de referencias. Se bloquea por tareas Pendiente/En curso o trabajo registrado sin finalizar, servicios Pendiente de entrega/Entregado y solicitudes Pendiente/Aprobada; el error identifica los elementos concretos. No se cancelan ni reasignan automáticamente.
- Al cambiar Empleado → Cliente se comprueban sus trabajos; al cambiar Cliente → Empleado se comprueban los servicios vigentes y solicitudes abiertas del perfil. Se conservan las referencias históricas. Ser responsable habitual por sí solo no impide la transición: la tarjeta mantiene su nombre y lo identifica como no habilitado si deja de ser empleado activo. No se le asignan trabajos nuevos.
- En cada petición la API valida el estado/rol actuales, además de firma y vencimiento del token. Una sesión previa no permite operar durante suspensión o baja. Ante 401 la app limpia la sesión. Después de reactivar se puede iniciar sesión normalmente; este bloque no agrega una lista de revocación permanente de tokens anteriores que aún no vencieron.

## API y confirmaciones

Todos los endpoints de gestión requieren administrador activo. Nunca devuelven hashes, contraseñas ni tokens. Detalle/listado agregan `motivoSuspension` y `fechaBaja`; el motivo mostrado es el último guardado, no un historial completo de transiciones.

- `POST /api/autenticacion/registro`: 201, `estadoCuenta: Activa`, mensaje «Cuenta creada. Ya podés iniciar sesión.». No devuelve token; login sigue siendo separado.
- `POST /api/cuentas/{id}/activar`: sin cuerpo; solo Pendiente Cliente/Empleado con perfil, 204. Una segunda activación da 409 y se vuelve a consultar el estado.
- `PUT /api/cuentas/{id}/rol`: `rolAnterior` y `nuevoRol`; mismos valores Cliente/Empleado. 204 al guardar, 409 por cambio concurrente o elementos bloqueantes. Estado intacto.
- `PUT /api/cuentas/{id}/estado`: `estadoAnterior`, `nuevoEstado` y `motivo` (solo suspensión).

Ejemplos sin credenciales:

```json
{ "estadoAnterior": "Activa", "nuevoEstado": "Suspendida", "motivo": "PRUEBA LOCAL: pausa temporal" }
```

```json
{ "estadoAnterior": "Suspendida", "nuevoEstado": "Activa", "motivo": null }
```

```json
{ "estadoAnterior": "Activa", "nuevoEstado": "BajaLogica", "motivo": null }
```

La baja también admite origen Pendiente o Suspendida. Respuestas: 204 guardado; 400 datos/transición inválidos; 401 sin sesión válida; 403 sin rol administrador activo; 404 cuenta inexistente; 409 estado cambiado, baja definitiva, protección propia/último administrador o trabajos/servicios/solicitudes bloqueantes. Repetir el mismo estado final no reemplaza fechas ni motivo. Los rechazos no muestran éxito. La app muestra el conflicto concreto y permite consultar el estado actual. Gestionar cuentas conserva sus pestañas Cliente/Empleado y agrega los botones y confirmaciones dentro del detalle existente, sin otra pantalla.

## Migración local

`20261108193729_RegistroClienteActivo` cambia **únicamente el DEFAULT** de USUARIOS.EstadoCuenta de Pendiente a Activa. Se revisó el SQL aplicado: reemplaza la restricción DEFAULT y registra la migración, sin UPDATE de filas. No agrega columnas ni tablas. La API también asigna Activa explícitamente, sin depender del DEFAULT.

Aplicada solo a `(localdb)\MSSQLLocalDB` / `BanosQuimicosDb`: antes y después, 5 cuentas Activas y 2 Pendientes; 12 tablas del negocio más __EFMigrationsHistory. No se activaron, suspendieron ni dieron de baja cuentas existentes para probar este bloque. **Pendiente para hosting**, junto con las migraciones anteriores que falten allí. No se conecta ni modifica MonsterASP.

## Guía Windows — tres recorridos aprobados; alcance exacto en el cierre

1. Detener una API anterior antes de compilar. Abrir BanosQuimicos.sln en Visual Studio 2026. Ejecutar la API con perfil https y la app en Windows. ConexionApi conserva UsarHosting=false: Windows usa https://localhost:7292; confirmar peticiones en la API local. No usar cuentas ni URL de MonsterASP para este bloque.
2. Registrar una cuenta nueva identificada **PRUEBA ESTADOS LOCAL** con un correo único y contraseña elegida personalmente, sin guardarlos en documentación. Debe informar cuenta creada y permitir login inmediatamente, sin espera de aprobación. Al ingresar debe mostrar Cliente/Activa. Sin servicios propios, la lista estará vacía.
3. Cerrar sesión; entrar como administrador local. Gestionar cuentas → buscar el correo → Ver detalle: debe estar Activa y sin botón de activación. Cancelar primero la confirmación de suspensión para comprobar que no guarda. Reabrirla: motivo vacío impide confirmar. Ingresar **PRUEBA LOCAL: pausa temporal** y confirmar. Esperar confirmación real y Suspendida en detalle/listado.
4. Para comprobar una sesión previa, obtener antes de suspender una sesión de ese cliente en otra ventana o Postman, conservando el token solo localmente. Después de suspender, consultar `/api/autenticacion/yo` con ese token: 401. En la app, intentar consultar datos debe invalidar la sesión; login con contraseña correcta debe indicar cuenta no habilitada. No copiar tokens al chat, Git ni capturas de la guía.
5. Como administrador, detalle de la cuenta de prueba Suspendida → Reactivar cuenta → confirmar: Activa, mismo rol y datos. El cliente puede ingresar con las mismas credenciales. Repetir con rol Empleado si se desea: cambiar el rol desde la tarjeta **antes de darle tareas**, iniciar sesión para comprobar Empleado y luego suspender/reactivar. El cambio de estado nunca cambia el rol.
6. Sobre esa cuenta nueva y sin operaciones abiertas, Dar de baja la cuenta: leer la advertencia «no podrá reactivarse desde la app», cancelar una vez y luego confirmar. Debe quedar Baja lógica con fecha, datos conservados y sin opción de reactivación. Login queda rechazado; un token anterior no permite `/yo`. No dar de baja cuentas anteriores del estudiante para preparar esta prueba.
7. Bloqueos: con un empleado que tenga un trabajo de prueba activo, intentar suspender/dar de baja/cambiar a Cliente. Debe rechazar mostrando el ID y estado del trabajo, sin alterar nada. Con un cliente que tenga servicio vigente o solicitud abierta, intentar deshabilitar/cambiar a Empleado: debe mostrar los elementos bloqueantes, sin cancelarlos. Usar los recorridos existentes para resolverlos si corresponde; no cambiar estados directamente en SQL. Responsable habitual sin trabajos activos se conserva al deshabilitar y deja de aparecer entre empleados asignables.
8. Si se quiere revisar la compatibilidad con una cuenta antigua Pendiente de prueba existente: confirmar primero su identidad, abrir detalle como administrador y activarla con su rol Cliente o Empleado. No hace falta crear pendientes nuevos por SQL. Las pruebas automatizadas preparan pendientes antiguos solo en bases aisladas, nunca en la base del estudiante.

No hay credenciales fijas ni cambios de contraseñas/roles de cuentas anteriores como preparación automática. Las cuentas nuevas para esta guía las crea el estudiante; los escenarios automatizados usan bases BanosQuimicosAuthPruebas_GUID y datos propios aislados.

## Verificaciones y pendientes

Resultados automatizados y compilaciones del bloque: sección 28 del plan. No equivalen a una ejecución visual en Windows/Android. Tres recorridos manuales Windows aprobados según el cierre, sin extender la aprobación al resto de la guía; integral Android contra API local al terminar los bloques, vencimiento real del token y alternancia de servidores en dispositivo siguen pendientes. Migraciones y republicación se realizan después de Android integral y correcciones. Sin commit, push, merge ni cambios en hosting.


Evidencia automatizada de cierre: **13 casos nuevos y suite completa 217/217 correcta** (204 anteriores conservados). Compilación API/Razor y app Debug Windows/Android correctas, 0 errores/advertencias. La comprobación completa anterior de inventario y el filtro no se presentan como prueba de esta nueva regla de cuentas. Aprobación manual limitada a los tres recorridos detallados en el cierre, separada de estos resultados.


## Cierre: pruebas manuales Windows aprobadas — 08/11/2026

El estudiante confirmó únicamente estos tres recorridos, registrados como **pruebas manuales Windows**, separados de las pruebas automatizadas:

1. Registro como Cliente/Activa sin aprobación y cambio a Empleado.
2. Suspensión y reactivación conservando el rol.
3. Baja lógica y pérdida de acceso.

No se atribuye aprobación manual a otros escenarios de la guía: bloqueos por tareas/servicios/solicitudes, activación de cuentas antiguas Pendientes, motivo vacío, cancelación de confirmaciones, permisos, concurrencia o protección de administradores conservan exclusivamente las evidencias automatizadas que correspondan, no una confirmación manual nueva. Tampoco se afirma una prueba manual específica de token previo por el solo reporte de pérdida de acceso.

**Compilación anterior confirmada:** el proceso finalizó con código 0, compilación Debug Windows y Android correcta, **0 errores y 0 advertencias**. Esa ejecución ya registrada no quedó pendiente; no se repitió para este cierre. API/Razor compilaron con las pruebas. Se mantienen **217/217 automatizadas correctas, 13 casos nuevos** como evidencia previa, separada de estos resultados manuales. EF confirmó que no quedan cambios del modelo sin migración.

**Bloque feature/estado-cuentas cerrado y listo para commit**, sin iniciar otro bloque. Solo se actualizan el plan y la guía; no se ejecutan pruebas, compilaciones ni cambios de datos/configuración. La migración **20261108193729_RegistroClienteActivo** sigue aplicada solo en LocalDB y **pendiente para hosting**, junto con las anteriores que falten. Android integral contra API local, correcciones y posterior migraciones/republicación/comprobación contra hosting se mantienen para el final. Vencimiento real del token y alternancia de servidores siguen pendientes generales.

Mensaje de commit sugerido: **feat: habilitar registro activo y gestionar estados de cuentas**.

Descripción breve sugerida del PR: «El registro crea Cliente/Activa y permite el acceso sin aprobación administrativa. Gestionar cuentas incorpora suspensión con motivo, reactivación y baja lógica, con bloqueos por operaciones abiertas y protección de administradores. Conserva datos y valida el estado actual de las sesiones. Verificado con 217 pruebas automatizadas y compilación Windows/Android; aprobados manualmente en Windows el registro/cambio de rol, suspensión/reactivación y baja/pérdida de acceso. La migración del DEFAULT está aplicada solo en LocalDB; Android integral y despliegue quedan pendientes.»

Sin commit, push, merge ni cambios en hosting. Esperar confirmación antes de comenzar el siguiente bloque.
