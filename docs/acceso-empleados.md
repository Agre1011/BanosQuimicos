# Acceso de empleados mediante cambio de rol

El recorrido confirmado reemplaza la idea de alta directa: cada persona se registra en el formulario público, obtiene Cliente/Pendiente y conserva su contraseña. Un administrador activo puede cambiar Cliente ↔ Empleado sobre esa misma cuenta. **Rol y estado son independientes:** el cambio no activa, suspende ni da de baja. No se crea otro usuario ni otro perfil.

## Interfaz y referencia aprobada

La captura de Clientes y Empleados de la página 6 permite distinguir dos pestañas y un desplegable en las tarjetas. La vista existente incorpora pestañas Clientes/Empleados y selector de rol por tarjeta, con confirmación que muestra el estado sin modificarlo. Se conservan búsqueda, estado, paginación, detalle y activación del bloque previo. Al cambiar de rol, la cuenta aparece en la otra pestaña. No se agregó otra pantalla administrativa ni un formulario de alta.

El inicio Empleado corresponde a la página 5: saludo destacado, acceso a Tareas asignadas y Cerrar sesión. En este bloque el acceso a tareas queda deshabilitado e identificado como pendiente de su implementación: no se inventan cantidades ni tareas. No ofrece gestión administrativa. El login y /yo determinan el rol actual; un token antiguo también refleja cambios en la siguiente petición.

## API

`PUT /api/cuentas/{idUsuario}/rol`, Bearer de administrador activo:

```json
{
  "rolAnterior": "Cliente",
  "nuevoRol": "Empleado"
}
```

La vuelta usa Empleado → Cliente. Solo se aceptan esos dos valores y roles distintos; no se puede asignar Administrador ni cambiar una cuenta que sea Administrador. Campos JSON extra, incluido estadoCuenta, se rechazan. El UPDATE modifica únicamente IdRol y condiciona el rol anterior para detectar otra sesión. La aplicación vuelve a consultar el listado después del cambio o conflicto; no reintenta automáticamente la escritura.

- 204: cambio realizado; se mantienen estado, ID, contraseña, datos de contacto, fecha de registro, perfil e historial.
- 400: valores inválidos, mismo rol o campos adicionales.
- 401: sin token válido o administrador suspendido/dado de baja.
- 403: cliente, empleado o cuenta Pendiente sin autorización administrativa.
- 404: usuario inexistente.
- 409: rol anterior desactualizado o cuenta fuera de Cliente/Empleado.

El listado admite `rol=Cliente` o `rol=Empleado`, combinado con búsqueda/estado/paginación y orden estable existentes. Omitir rol mantiene el listado general de API. Las pestañas de la app solicitan este filtro al servidor; no filtran solo la página descargada.

La activación existente permanece `POST /api/cuentas/{id}/activar` para Cliente/Pendiente. **Para el recorrido de este bloque, activar antes de asignar Empleado.** Cambiar un Pendiente a Empleado no lo habilita; la restricción sigue aplicándose. Si se hizo en ese orden, volver a Cliente antes de usar la activación existente. No se amplió el contrato de activación.

## Preparar exclusivamente el entorno local

1. En ConexionApi.cs está seleccionado `UsarHosting = false`. Recompilar después de cambiarlo. Windows usará https://localhost:7292; Android Debug http://127.0.0.1:5148 por USB. No seleccionar hosting: esta función nueva todavía no está publicada.
2. En una terminal PowerShell nueva, desde la raíz, fijar explícitamente LocalDB solo para esa terminal y sus procesos. Evita que una configuración externa de la preparación de hosting reemplace el destino:

   ```powershell
   $env:ConnectionStrings__BanosQuimicos = 'Server=(localdb)\MSSQLLocalDB;Database=BanosQuimicosDb;Trusted_Connection=True;TrustServerCertificate=True;'
   dotnet ef dbcontext info --project BanosQuimicos.Api --no-build -- --environment Development
   ```

   Esperado: Database name BanosQuimicosDb y Data source (localdb)\MSSQLLocalDB. Esto comprueba la configuración seleccionada; no identifica una base por la respuesta pública de /api/estado.
3. En esa misma terminal: `dotnet run --project BanosQuimicos.Api --launch-profile https`. Esperado: escucha HTTPS 7292 y HTTP 5148 en localhost. Comprobar https://localhost:7292/api/estado: 200. Usar el administrador **local** existente; las cuentas del hosting no se copian. Si no existe uno, seguir el comando interactivo de docs/autenticacion-api.md, después de verificar LocalDB, con credenciales propias y fuera del chat.
4. Abrir BanosQuimicos.App en Visual Studio, Debug, Windows Machine o Moto G9 Plus autorizado por USB. Para Android ejecutar `adb reverse tcp:5148 tcp:5148` desde el SDK y comprobar `adb reverse --list`. No cambiar firewall ni certificados. Mantener la API local activa.
5. Para verificar el servidor de la app, poner un punto de interrupción al retornar CrearCliente en ConexionApi.cs y observar `direccion` o BaseAddress: deben ser los destinos locales indicados, nunca celeris.runasp.net. El registro de peticiones de la API local debe reflejar las llamadas. El cambio de URL invalida la restauración de la sesión alojada según la lógica existente; no copiar tokens entre destinos.

## Recorrido manual y resultados esperados

1. Registrar una persona nueva identificada PRUEBA ROL LOCAL, con correo único, teléfono y contraseña elegida localmente. Esperado 201, Cliente/Pendiente y perfil cliente. Ingresar: solo aviso de espera, sin rol ni gestión.
2. Ingresar como administrador local, Gestionar cuentas → Clientes, buscar la cuenta y Ver detalle. Activar con confirmación. Esperado Activa; la contraseña no cambia.
3. Volver al listado, en la tarjeta seleccionar Empleado. **Elegir una opción todavía no guarda:** la tarjeta debe mantener «Rol guardado: Cliente» y mostrar «Cambio sin guardar: Cliente → Empleado». La confirmación aparece debajo del selector de esa misma tarjeta. Cancelar una vez: no debe cambiar nada. Repetir y pulsar **Confirmar cambio de rol**. Solo después de guardar y consultar la cuenta a la API aparece «Rol guardado: Empleado»; la cuenta sale de Clientes y aparece en Empleados, con estado Activa.
4. Cerrar sesión e ingresar con el correo y contraseña originales de esa persona. Esperado: inicio de empleado con saludo, Rol Empleado, Tareas asignadas deshabilitado para este bloque y Cerrar sesión. No debe aparecer Gestionar cuentas ni un formulario de alta administrativa.
5. Como administrador, devolver Empleado → Cliente desde la tarjeta. Con la misma cuenta al ingresar otra vez se muestra inicio Cliente; estado y perfil se mantienen. No se borran referencias históricas.
6. En Postman local, intentar PUT /api/cuentas/{id}/rol con token de cliente y luego empleado: 403. Listado y detalle administrativos también 403. Sin token: 401. Usar Vault; no guardar contraseñas ni tokens en documentos. El cliente público no puede mandar rol/estado privilegiados en el registro.
7. Con otro registro pendiente de prueba, cambiar a Empleado antes de activar y comprobar que sigue Pendiente sin acceso operativo. Devolver a Cliente para usar la activación existente. Esto comprueba que rol no equivale a estado.
8. Con dos sesiones del administrador, abrir el mismo rol previo y confirmar el mismo cambio: una operación puede devolver 204 y la segunda 409; debe refrescar el listado sin error inesperado. No se asegura detectar una secuencia completa de ida y vuelta que termine en el mismo rol: no hay versión de fila en este bloque.
9. Repetir en Windows y Android; comprobar tarjetas, selector, confirmación, teclado, pestañas, paginación e inicio de empleado con desplazamiento/rotación. Estas comprobaciones de interfaz requieren confirmación del estudiante; compilar no las certifica.

## Alcance de las verificaciones

Los tests ejecutan controladores y cliente/sesión C# reales mediante TestServer con SQL Server LocalDB, en bases nuevas BanosQuimicosAuthPruebas_<GUID> conservadas. Los secretos se generan en memoria. El historial de prueba se crea solo allí; no se modifica BanosQuimicosDb ni db77593 para estos tests. Incluyen cambio ida/vuelta, hash/perfil/fecha y tarea realizada conservados, estados independientes, autorización, roles inválidos/Administrador, intento de inyectar estado, filtros y escrituras concurrentes. No equivalen a ejecutar la interfaz MAUI.

No se necesita migración: se conservan las 12 tablas. La API y la app requerirán publicación/distribución futura para usar el nuevo endpoint fuera de local. No se publicó ni se ejecutó SQL en MonsterASP. Siguen pendientes el vencimiento real del token y la prueba de alternar servidores en el dispositivo hasta que el estudiante las confirme.

### Compilación en este equipo con memoria limitada

Windows compiló normalmente. Android D8 falló al reservar memoria nativa con Java y heap 1G; el informe de la JVM confirmó memoria insuficiente. Se completó con esta ejecución temporal, sin modificar csproj ni instalar herramientas:

```powershell
dotnet build BanosQuimicos.App --no-restore -f net10.0-android -m:1 -p:JavaMaximumHeapSize=512M '-p:JavaOptions=-XX:+UseSerialGC'
```

Si Visual Studio vuelve a mostrar el mismo fallo de memoria, liberar memoria de aplicaciones que no se necesiten o usar este comando para compilar. No confundirlo con un error de C#/Razor. No se modificó permanentemente la memoria virtual ni la configuración Java del equipo.

## Corrección de la prueba manual: selección frente a guardado

El estudiante informó un fallo: eligió Empleado en la tarjeta y al ingresar seguía siendo Cliente. Confirmó después que no había pulsado Confirmar cambio de rol. El selector anterior mostraba el valor propuesto mientras la confirmación estaba arriba del listado; esa presentación hacía parecer guardado un cambio pendiente. Se reabrió el bloque y se registró la prueba manual como fallida; no se atribuyó a una contraseña ni a un rol viejo en el JWT.

La corrección mantiene visible el rol guardado, identifica el cambio sin guardar y ubica Confirmar/Cancelar dentro de la misma tarjeta. El éxito se informa después del PUT y de consultar el rol actual a la API. No se autoactiva ni se guarda solo por seleccionar.

La regresión nueva renderiza los Razor reales con HtmlRenderer de ASP.NET Core y ejecuta sus callbacks en el dispatcher de Blazor; usa API real en TestServer y SQL Server LocalDB aislado. Para un mismo IdUsuario de prueba se comprobó:

| Etapa | Antes de confirmar | Después de confirmar y entrar otra vez |
| --- | --- | --- |
| Rol persistido en SQL Server | Cliente | Empleado |
| Login nuevo | Token del mismo IdUsuario, sin campo de rol | Token nuevo del mismo IdUsuario, sin campo de rol |
| GET /api/autenticacion/yo | Cliente | Empleado |
| SesionUsuario.Cuenta.Rol | No evaluada en este paso | Empleado |
| Home.razor | No renderizado en este paso | InicioEmpleado, Rol: Empleado, sin Gestionar cuentas |

El login no devuelve rol: el JWT identifica al usuario y /yo aporta el rol actual que utiliza la app. La tabla resume una cuenta de prueba aislada, no una consulta de credenciales del estudiante. El renderer verifica componentes y callbacks reales, pero no reemplaza repetir los clics, el desplazamiento y la visualización en Windows/Android. Los pendientes de vencimiento real y alternar servidores en dispositivo continúan abiertos.
