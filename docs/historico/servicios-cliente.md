> Documento histórico archivado el 08/11/2026. Conserva decisiones, resultados y pendientes de su etapa; no es una guía vigente. Las actualizaciones posteriores pueden sustituir sus reglas. Consultar el [plan actual](../plan-trabajo.md) y el [índice histórico](indice.md).

# Consulta de servicios del cliente

**Alcance vigente (08/11/2026):** para necesitar otro baño se usa **Solicitar un nuevo servicio**, pudiendo repetir la dirección. No hay adicionales vinculados, agrupación de contrataciones ni herencia de datos. Los servicios resultantes son independientes, con sus propias frecuencias, tareas, traslados y bajas. Esta decisión reemplaza las propuestas previas de adicionales; no cambia el recorrido existente ni elimina tipos o registros históricos.


Bloque en `feature/servicios-cliente`, exclusivamente con API local y LocalDB. Recorrido: Inicio del cliente → Mis Servicios → Detalle → Volver al listado → Volver al inicio → Cerrar sesión.

## Reglas y alcance

Un servicio **vigente** conserva la asignación del baño: estado **Pendiente de entrega** o **Entregado**, según C8 del plan y el índice de asignaciones del modelo. Ambos cuentan y aparecen en Mis Servicios. Finalizado y Cancelado quedan fuera de esta consulta; se conserva su historial. Pedir/programar un retiro no resta el servicio: deja de contar al finalizarlo. La condición No apto o un mantenimiento no ocultan una asignación vigente.

El nombre procede de `/api/autenticacion/yo`. Contador, tarjetas y detalle proceden de la API. El contador no tiene valor hasta recibir una respuesta válida: carga/error nunca se convierten en cero. Cero se muestra solo si la consulta fue correcta y no hay servicios vigentes. Volver del detalle vuelve a consultar el listado; volver al inicio vuelve a consultar el contador.

Tarjetas: código, tipo de baño/uso, dirección y limpiezas por semana. El detalle agrega el estado. Dirección actual o preparada del **mismo servicio**, sin tomar una ubicación ajena por compartir baño. Si no existe, indica que no hay dirección registrada. Frecuencia cero indica que no se han previsto limpiezas semanales; sigue siendo una referencia manual, sin generación automática.

Se conservan visibles y deshabilitados Mis Solicitudes, Solicitar un nuevo servicio y las acciones de limpieza extra, cambio de dirección, reclamo y baja. No envían peticiones ni muestran éxitos simulados. Las modalidades «Ya realicé el cambio de dirección» y «Necesito que lo trasladen» permanecen diferenciadas en el texto, sin implementar sus formularios.

Se revisaron el PDF aprobado (texto y página 7 visual), las capturas aportadas, DER, plan y código. Se reutilizan fondo, tarjetas, colores y navegación de la referencia y la marca actual de la aplicación. El repositorio no contiene un logo Celeris independiente: no se creó ni sustituyó por un gráfico inventado. El recorrido manual Windows fue confirmado por el estudiante en el cierre siguiente.

## API y seguridad

Todas las rutas requieren **Cliente/Activa**, comprobados desde la cuenta actual en cada petición:

- `GET /api/servicios-cliente/mios/resumen`: `{ "cantidad": 3 }` como ejemplo, sin identidad adicional.
- `GET /api/servicios-cliente/mios`: lista propia de vigentes, orden estable por código e ID.
- `GET /api/servicios-cliente/{id}`: detalle propio y vigente; 404 si es ajeno, inexistente o dejó de estar vigente.

No se recibe un identificador de cliente. Se filtra por el usuario autenticado y su relación CLIENTES. Ni administrador ni empleado pueden usar estas rutas; utilizan sus propias consultas. No se devuelven hashes, credenciales, contacto ajeno, observaciones administrativas ni tokens. Respuestas sin caché. Reutiliza manejo de sesión, HTTPS y servidor fijo; 401 cierra la sesión y 403/falta de conexión impiden mostrar identidad operativa sin verificar. No agrega tablas, entidades ni migraciones.

## Preparación y prueba manual Windows

1. Abrir `BanosQuimicos.sln` en Visual Studio 2026. `Plataforma/ConexionApi.cs` ya conserva `UsarHosting = false`: Windows usa `https://localhost:7292/`. No cambiar certificados ni apuntar al hosting. Iniciar la **API actualizada**, con el perfil HTTPS local, antes de abrir la app Windows Debug.
2. Ingresar con las credenciales locales que ya conocés de **Cliente Prueba** (IdUsuario 2). No se cambiaron contraseñas ni roles. Consulta de solo lectura de preparación: tiene **3** servicios vigentes: `PRUEBA-TAREAS-01`, `PRUEBA-NUEVO-01` y `PRUEBA-NUEVO-02`. Los retiros `PRUEBA-RETIRO-01/02` están Finalizados y no cuentan. Si realizás otras operaciones después, la cantidad puede cambiar legítimamente.
3. Comprobar nombre real y contador 3 → Mis Servicios → tres tarjetas con datos reales → Ver detalle. Comparar código, tipo/uso, dirección, frecuencia y estado. Volver al listado y al inicio. Comprobar que las opciones pendientes no se pueden pulsar ni notifican éxito. Reducir la ventana y verificar desplazamiento y textos largos.
4. Cerrar sesión e ingresar con una cuenta Cliente/Activa sin servicios. La consulta local identificó **Empleado Prueba** (IdUsuario 3, pese al nombre su rol actual es Cliente) y **Agus Tin** (IdUsuario 5). Usar solo una cuya contraseña ya conozcas; no restablecerla ni cambiar su rol. Debe mostrar contador 0 y listado vacío, sin datos de la sesión anterior. Si no disponés de esas credenciales, registrar personalmente otra cuenta local y activarla con el administrador existente.
5. Para pérdida de conexión, con la app abierta detener solo tu API local e intentar consultar. Debe mostrar error/sesión sin verificar, nunca afirmar que hay cero servicios. Reiniciar la API y consultar nuevamente el estado; debe recuperar los datos. Cerrar sesión, reabrir la app y comprobar login.

### Resultado manual Windows: aprobado

El estudiante confirmó:

- **Cliente Prueba:** contador de 3 servicios, listado, detalle y navegación de regreso correctos.
- **Cliente sin servicios:** mensaje de listado vacío, sin datos del cliente anterior.
- **Acciones futuras:** visibles y deshabilitadas.

Estas son verificaciones **manuales**, separadas de las **155 pruebas automatizadas anteriores**. No se atribuye a esta confirmación pérdida/recuperación de conexión, reinicio, cierre de sesión persistente ni comprobaciones específicas de ventana reducida; esos pasos de la guía no fueron confirmados en este cierre. No se repitieron pruebas ni compilaciones por esta actualización documental.

## Pendientes

Integral manual Android al terminar los bloques, contra API local: `adb reverse tcp:5148 tcp:5148`, app Debug con conexión local existente y sin cambios de HTTPS globales. Acciones de solicitudes del cliente en bloques posteriores. Vencimiento real del token y alternancia de servidores en dispositivo siguen pendientes. Después de la validación integral se planificará republicación y migraciones previas del hosting; este bloque no genera otra migración ni modifica MonsterASP.

## Verificaciones automatizadas y cierre

Se conservaron los 147 casos anteriores y se agregaron 8 casos de reglas/seguridad en `ServiciosClientePruebas.cs`, sin pruebas nuevas de detalles visuales simples. Primera pasada focalizada: 7 correctos/1 fallido por filtro de detalle después de proyectar el DTO; corregido para filtrar antes de proyectar. Segunda pasada: 8/8 correctos. Una única suite completa de cierre: **155 correctos, 0 fallidos, 0 omitidos**. Total **171 ejecuciones de casos**, incluidos los dos recorridos focalizados y el fallo corregido; 155 casos distintos.

La API y los componentes Razor compilaron con las pruebas. La app Debug Windows y Android compiló con **0 errores y 0 advertencias**, usando solo parámetros temporales de Java ya documentados. TestServer y bases LocalDB aisladas prueban la API real; no equivalen a ejecutar la app nativa. El recorrido manual Windows indicado arriba está aprobado; la integral manual Android queda pendiente para el final, antes de republicar. Revisión de diff/candidatos sin archivos generados, perfiles privados ni indicadores de secretos; originales conservados. **Bloque cerrado y listo para commit**, sin commit, push, merge, publicación ni inicio del siguiente. Mensaje sugerido: `feat: consultar servicios propios del cliente`.


## Actualización: paginación y contadores

La pantalla ahora consulta /api/servicios-cliente/mios/pagina con páginas de diez, total y navegación. Las rutas de lista anteriores se conservan por compatibilidad; los filtros se aplican antes de paginar en la API y no a una página aislada. Ver [guía Windows actual por grupos](paginacion-contadores-logo.md). Las aprobaciones manuales anteriores no acreditan la paginación nueva.
