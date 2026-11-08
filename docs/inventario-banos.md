# Inventario de baños: guía local

Bloque `feature/inventario-banos`, desde master `3503ed6`. API local y LocalDB; no se publica ni modifica MonsterASP. Adaptaciones autorizadas: alta sencilla dentro de Estado Baños, filtro de bajas, confirmaciones de condición/baja y programación de mantenimiento sin servicio en la misma tarjeta. Se conservan tarjetas, estilos y navegación existentes; no hay pantallas nuevas ni modificación de originales.

## Comportamiento

- **Agregar baño:** solo se elige Obra/Evento del catálogo. La API genera B.O-/B.E- y el siguiente número histórico de ese prefijo, con un mínimo de dos dígitos. Incluye baños dados de baja y no aplica los ejemplos 40/50 como límites. La longitud técnica existente del código sigue siendo 30 caracteres. Nace Apto, sin servicio ni responsable habitual; disponible.
- **Condición física:** Apto/No apto, con confirmación administrativa. No cierra servicios, no cancela trabajos y no elimina mantenimientos. Finalizar un mantenimiento nunca cambia esa condición.
- **Baja del inventario:** fecha persistida, sin eliminación ni reactivación en este bloque. Se impide si existe servicio Pendiente de entrega/Entregado o cualquier tarea Pendiente/En curso, con sus IDs, tipos y estados en el error. Se conserva código e historial; queda fuera de nuevas asignaciones y trabajos. Filtro Activos/Bajas/Todos para consultarlo. No confundir con retirar un servicio.
- **Mantenimiento sin servicio:** baño activo de inventario sin servicio vigente, empleado activo, fecha y descripción de 1 a 2000 caracteres sin aceptar espacios solos. El administrador indica si bloquea nuevas asignaciones. No exige Apto ni disponibilidad: sirve justamente para reparar un baño No apto. No altera responsable habitual. Genera Mantenimiento/Pendiente y reutiliza consulta/registro/finalización del empleado.
- **Disponibilidad:** inventario activo + Apto + sin servicio vigente + sin mantenimiento bloqueante Pendiente/En curso. La tarjeta muestra condición y bloqueo por separado. El detalle del empleado muestra instrucciones, condición actual y cantidad de otros mantenimientos bloqueantes; no inventa observaciones ni reposición de papel.

Los endpoints administrativos requieren administrador activo y vuelven a comprobarlo dentro de la transacción. Se reutiliza el bloqueo transaccional compartido por asignaciones y cierres para evitar carreras con baja/programación. La base mantiene el índice único de código. Alta usa ClaveAlta y mantenimiento usa ClaveProgramacion: repetir la clave con los mismos datos recupera el resultado; cambiar su contenido se rechaza. Una nueva operación intencional usa otra clave. La app impide doble toque, conserva el envío incierto en memoria durante la sesión y permite reintentarlo sin cambiar los datos. No guarda contraseñas ni borradores en disco; si se cerró completamente la app con un envío incierto, consultar el listado antes de iniciar un alta/programación nueva.

## Migración y datos

`20261108174906_InventarioBanosYMantenimiento`, revisada y aplicada **solo** a `(localdb)\MSSQLLocalDB` / `BanosQuimicosDb`:

- BANOS_QUIMICOS: FechaBaja nullable para distinguir baja lógica de condición; ClaveAlta nullable con índice único filtrado `IS NOT NULL`, compatible con las filas anteriores sin clave.
- TAREAS_OPERATIVAS: DescripcionTrabajo nullable, nvarchar(2000), instrucciones del administrador distintas de ObservacionesEmpleado.
- CHECK: FechaBaja nula o no anterior a FechaAlta.

Es aditiva: no crea tablas, no elimina ni actualiza registros existentes y no cambia catálogos. Verificación posterior: 12 tablas del negocio + __EFMigrationsHistory, 27 filas de catálogos, 8 baños y 22 tareas conservados; los tres campos nuevos están NULL en los datos anteriores. Pendiente para futura publicación en hosting, junto con las migraciones anteriores que falten. No ejecutar Down para preparar una prueba: eliminaría las columnas y su información nueva.

## Recorrido manual Windows — aprobado por el estudiante

1. Detener una API anterior antes de compilar desde Visual Studio 2026. Abrir BanosQuimicos.sln, ejecutar API con perfil https y después App/Windows. ConexionApi conserva UsarHosting=false: Windows consulta https://localhost:7292, no celeris.runasp.net. Usar las credenciales locales personales; no escribirlas en la guía.
2. Como administrador: Inicio → Estado Baños → Agregar baño → elegir Obra → Confirmar alta. Anotar el **código generado** como «baño de prueba inventario» (no hay un código fijo preparado). Debe figurar Apto, Disponible, sin servicio y sin responsable. Consultar nuevamente y comprobar persistencia. Si se desea comprobar Evento, crear otro baño nuevo y anotar también su código; no dar de baja unidades de pruebas anteriores.
3. En la tarjeta del baño nuevo: Cambiar a No apto → Confirmar condición. Debe conservar su código y quedar no disponible. Programar mantenimiento sin servicio: elegir un empleado activo local, la fecha deseada y descripción **PRUEBA LOCAL INVENTARIO: revisar bisagras**, marcar bloqueo y confirmar. Anotar el **IdTarea real** mostrado. La tarjeta debe seguir No apto y mostrar mantenimiento bloqueante.
4. Intentar Dar de baja del inventario → confirmar. Debe rechazarse indicando esa tarea y su estado. No borra el baño ni cancela el trabajo. Cancelar una confirmación de condición/baja no debe guardar cambios.
5. Cerrar sesión, entrar como empleado asignado: Tareas asignadas → tarea anotada → Ver detalle. Debe mostrar el código y la descripción exacta. Sin servicio/cliente/dirección asociados, no debe inventarlos. Registrar Trabajo y Actualizar Estado. Debe quedar Realizado y conservar el registro al volver a entrar.
6. Volver como administrador → Estado Baños → Consultar nuevamente. El mantenimiento deja de bloquear, pero la condición debe seguir **No apto**. Pulsar Confirmar Apto → confirmar: solo ahora quedará Disponible si no hay otros bloqueos. Si se programó otro mantenimiento bloqueante, la condición Apto por sí sola no lo elimina; completar el otro trabajo antes de esperar disponibilidad.
7. Dar de baja del inventario del **mismo baño nuevo**, confirmar. Desaparece de Activos; seleccionar Bajas/Todos para reconocerlo por código y fecha. Su historial de tarea permanece. No admite asignación de empleado, servicio ni mantenimiento nuevo. Crear luego otro baño del mismo tipo debe generar un código distinto, sin reutilizar el anterior.
8. Opcionalmente comprobar falta de conexión y recuperación con Consultar nuevamente. Un error no es un listado vacío ni una confirmación de guardado. Ante un envío incierto, reintentar el mismo; no cerrar la app y repetir como operación nueva sin consultar primero.

No se prepararon filas operativas en la base principal ni se modificaron cuentas, contraseñas o trabajos anteriores: estos pasos generan sus propios datos identificables por código, descripción e ID. Las pruebas automatizadas usan bases nuevas BanosQuimicosAuthPruebas_GUID, nunca la base del estudiante.

## Evidencias y pendientes

Verificado automáticamente: 13 casos nuevos y suite completa **203/203 correcta**; desarrollo focalizado de 15 existentes + 11 nuevos (229 ejecuciones acumuladas, 203 casos distintos). API/Razor compilaron con las pruebas; app Debug Windows/Android compiló con 0 errores y 0 advertencias. EF confirmó que no quedan cambios del modelo sin migración. No equivalen a la prueba manual de Windows ni de Android. Pendientes generales: integral Android contra API local al terminar los bloques, vencimiento real del token y alternancia de servidores en el dispositivo. Para Android local Debug sigue vigente `adb reverse tcp:5148 tcp:5148`; sin desactivar HTTPS globalmente. Republicación/migraciones de hosting solo después de la validación integral y correcciones.


## Confirmación manual y ajuste de filtro — 08/11/2026

**Aprobado manualmente en Windows**, separado de las 203 pruebas automatizadas anteriores:

- Alta y rechazo de baja mientras existe mantenimiento pendiente.
- Finalización por el empleado conservando No apto hasta confirmación administrativa de Apto.
- Baja lógica y consulta mediante el filtro de bajas.

No se atribuyen otras verificaciones manuales no informadas. Estos tres pasos ya no están pendientes.

Se agregó Condición física: **Todos / Aptos / No aptos**, combinado con Inventario: Activos/Bajas/Todos. La consulta completa se filtra en SQL mediante la API; el listado actual no tiene paginación ni recortes previos. No filtra por disponibilidad: un Apto asignado sigue figurando entre Aptos. Ambas selecciones permanecen al pulsar Consultar nuevamente y al refrescar después de una operación. Sin resultados se muestra «No hay baños que coincidan con los filtros de inventario y condición seleccionados»; un error mantiene su aviso de error.

**Filtro aprobado manualmente en Windows:** Todos / Aptos / No aptos, combinación con bajas, mensaje sin coincidencias y conservación de ambas selecciones al actualizar. La siguiente lista queda como guía para repetirlo, sin pruebas manuales pendientes de este bloque en Windows:

1. Estado Baños: seleccionar Activos y Aptos. Comprobar que todos son Aptos, incluso un baño con servicio asignado/no disponible si existe en los datos.
2. Seleccionar No aptos; luego combinar con Bajas y Todos de inventario. Solo deben mostrarse las coincidencias de ambos filtros.
3. Pulsar Consultar nuevamente: ambas selecciones deben conservarse. Si una combinación no tiene datos, debe aparecer el mensaje de coincidencias vacías, sin confundirlo con un error de conexión.
4. Volver a Todos de condición para recuperar todas las condiciones del inventario seleccionado.

No requiere migración ni datos nuevos en la base del estudiante. La migración de inventario pendiente para hosting y los pendientes generales Android integral, vencimiento real del token, alternancia de servidores y republicación se conservan.

Verificación automática del ajuste: **1 caso nuevo correcto**, comprobando las nueve combinaciones y que Apto no equivale a Disponible. API y Razor compilaron. Suite anterior 203/203 conservada como evidencia previa, sin repetirla; 204 casos definidos en el proyecto. El intento inicial falló antes de ejecutar el caso por el arranque de LocalDB; el reintento por el nombre normal pasó después de la recuperación documentada. No se repitieron compilaciones Windows/Android ni se generaron migraciones.


## Cierre del bloque — 08/11/2026

El estudiante confirmó correctamente el filtro de condición, su combinación con bajas, el mensaje sin coincidencias y la conservación de ambas selecciones al actualizar. Esta evidencia es **manual Windows**, separada de la suite previa 203/203 y de la regresión focalizada posterior 1/1 (204 casos definidos; no se afirma una ejecución completa de 204). Junto con los recorridos de inventario ya aprobados, **bloque cerrado y listo para commit**.

Solo se actualizan el plan y esta guía; no se repiten pruebas ni compilaciones. La migración **20261108174906_InventarioBanosYMantenimiento** permanece aplicada en LocalDB y **pendiente para hosting**. Android integral contra API local y republicación se mantienen para el final; vencimiento real del token y alternancia de servidores siguen pendientes generales. No se modifica el hosting ni se hace commit, push o merge. Mensaje sugerido: **feat: gestionar inventario y mantenimiento de baños sin servicio**.
