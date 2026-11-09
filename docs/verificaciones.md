# Verificaciones y límites

Este resumen conserva resultados informados/registrados, no ejecuta pruebas nuevas ni amplía aprobaciones. Detalles por etapa, fallos iniciales y correcciones están en el [plan histórico](historico/plan-trabajo.md) y [guías archivadas](historico/indice.md).

## Manuales Windows aprobadas por el estudiante

- Autenticación: validaciones, credenciales incorrectas, registro/correo duplicado, restauración y cierre de sesión, desconexión/recuperación. La activación de registros nuevos probada antiguamente fue sustituida por Cliente/Activa; antiguas Pendientes siguen soportadas.
- Gestión de cuentas: listado/búsqueda/filtros/detalle, cancelar activación y recuperación de conexión; activación desde dos sesiones comprobada contra hosting con Windows/Moto G9 Plus. Cambio confirmado Cliente → Empleado, filtro y login con mismo correo/contraseña muestran inicio de empleado. Seleccionar sin confirmar no era un cambio guardado.
- Tareas: inicio/listado/detalle/volver; registro y finalización persistidos; contador/filtro y paginación/regreso.
- Estado Baños: responsable habitual → limpieza programada para 07/11/2026 → empleado consulta/registra/finaliza.
- Solicitudes administrativas: limpieza extra y reclamo con mantenimiento, bloqueo/resolución y levantamiento del bloqueo. Nuevo pedido de dos baños: primera entrega mantiene Aprobada, segunda Resuelta.
- Retiro: bloqueo por limpieza pendiente, cancelación explícita/revisión administrativa y posterior cierre; mantenimiento bloqueante permanece después.
- Cliente: contador de 3 servicios/listado/detalle/volver; otro sin servicios sin datos anteriores; acciones futuras deshabilitadas en aquella etapa. Posteriormente se aprobaron nuevo pedido → entregas → nuevos servicios/contador/Resuelta, y limpieza/reclamo → tareas → Resueltas.
- Dirección/baja: traslado informado validado sin tarea; traslado **21** con origen/destino; retiro **22** registrado/finalizado; como **Agus Tin**, servicio fuera de Mis Servicios y sin vigentes. No confundir con otros escenarios/IDs del diagnóstico histórico.
- Inventario: alta/bloqueo de baja por mantenimiento; empleado finaliza manteniendo No apto hasta confirmación; baja lógica/filtro. Todos/Aptos/No aptos combinados con bajas, vacío y selección conservada al actualizar.
- Estado de cuentas: registro Cliente/Activa y cambio a Empleado; suspensión/reactivación conservando rol; baja lógica/pérdida de acceso. No extender a otros escenarios manuales de bloqueo no informados.
- Respuesta a reclamos: respuesta obligatoria y cierre; cliente ve Resuelto/texto/fecha sin tarea; tarea activa impide cierre administrativo y conserva registros.
- Ajustes finales: filtro físico Todos/Obra/Evento; logo solo login, registro y tres inicios, sin Celeris/BQ en listas/detalles; ausencia de Actualizar estado de la cuenta solo en inicio administrador.

## Manuales Android: aprobación parcial

Históricamente se aprobaron autenticación en Moto G9 Plus por USB (sesión, conexión, teclado/desplazamiento/rotación) y gestión/activación de cuentas contra HTTPS alojado, incluida otra sesión Windows. La confirmación más reciente contra API **local** aprueba USB, registro y login.

Eso **no aprueba la integral Android de la versión actual** ni las pantallas posteriores, todos los permisos o recorridos de negocio. Pendientes: integral local, vencimiento real del token y alternancia de servidores en dispositivo. Después, comprobación de versión republicada.

## Automatizadas y compilaciones anteriores

- Estado de cuentas: última suite completa registrada **217/217**; 13 casos nuevos en ese bloque.
- Respuesta de reclamos: **13 nuevos**, **60/60 afectados** (47 existentes). **230 definidos**, no ejecución completa de 230.
- Paginación/contador/logo: 10 nuevos; selección inicial **107 casos: 106 correctos y 1 expectativa antigua fallida**; tres casos focalizados ajustados pasaron. Resultado: 107 casos distintos afectados verificados, 110 ejecuciones acumuladas, sin repetir suite completa.
- Ajustes filtro físico/logo: **2 nuevas regresiones, 2/2**. Total al cierre: **242 definidos**, no una ejecución completa de 242. No sumar todas las ejecuciones históricas como si fueran casos distintos.
- Compilaciones anteriores Windows/Android de esos bloques: correctas, 0 errores/advertencias. El último botón solo tuvo compilación API/Razor, sin tests ni compilaciones de plataformas; su aprobación manual Windows llegó después.

El proyecto de pruebas usa API/JWT/SQL Server reales y bases nuevas `BanosQuimicosAuthPruebas_<GUID>` conservadas, no la base del estudiante. Parte del cliente/Razor se enlaza con almacén simulado: no certifica runtime MAUI, almacenamiento nativo, teclado o apariencia Android. Cantidades anteriores de cada bloque se conservan en historia; no se vuelve a ejecutar la suite por reorganizar documentos.

## Limpieza del proyecto

Rama `chore/limpieza-proyecto`, desde master limpio y coincidente con origin/master, `43d429f`. Retirados bot de plantilla, AboutAssets y CSS BQ sin consumidores; comentarios del proyecto simplificados conservando configuración. Sin lógica de negocio, transacciones, pruebas, migraciones o scripts modificados. Guías anteriores archivadas con aviso histórico y enlaces ajustados; documentos aprobados originales sin modificaciones.

Resultados de esta limpieza:

- **203 enlaces locales** comprobados, sin destinos inexistentes; **2 anclas** comprobadas. Sin referencias a las guías retiradas fuera del archivo histórico. No se comprueba disponibilidad de sitios externos.
- **23 antecedentes** comparados con HEAD: mismo contenido al excluir aviso histórico y ajuste de enlaces/rutas. No se perdieron resultados ni decisiones por consolidar. No se dejan archivos de transición innecesarios.
- PDF, DER, UML y PNG Celeris con hashes idénticos a HEAD. Pruebas, API, migraciones, scripts, plataformas, iconos/splash y AGENTS.md sin cambios. XML de configuración idéntico al quitar comentarios y la referencia específica al bot retirado.
- App Debug **Windows y Android: compilación correcta, 0 errores y 0 advertencias**. Windows completó normalmente. Los primeros intentos Android dentro del aislamiento generaron DLL pero terminaron con código 1; el diagnóstico mostró socket local de ADB denegado (10013). El reintento fuera de ese aislamiento completó correctamente, sin instalar/ejecutar la app ni tocar red del hosting. Los logs de diagnóstico quedaron en temporales, fuera del repositorio.
- Diff revisado sin errores de espacios; documentos nuevos sin espacios finales; índice Git vacío. Sin credenciales, tokens, perfiles privados ni generados entre los candidatos examinados.

No se ejecutaron pruebas automatizadas ni recorridos manuales nuevos; no se amplían las aprobaciones anteriores. Sin acceso a LocalDB/hosting, commit, push, merge o publicación. **Bloque cerrado y listo para revisión**, no iniciar otro sin confirmación. Revisión manual sugerida: abrir README y seguir enlaces a guías/pendientes; comprobar que cada documento histórico muestra su aviso.

Mensaje de commit sugerido: `chore: consolidar documentación y retirar recursos de plantilla sin uso`.
