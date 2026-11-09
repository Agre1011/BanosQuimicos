# Guía del empleado

Iniciar API/app según [ejecución local](ejecucion-local.md). Una persona se registra normalmente; administración cambia su rol a Empleado confirmando la operación. Usa el mismo correo/contraseña. No hay contraseña inicial entregada por administrador. Rol y estado son independientes.

## Inicio → Tareas asignadas → Detalle → Volver

El inicio muestra nombre/rol actuales y cantidad de **tareas sin finalizar de hoy en Argentina (UTC−3)**. Cuenta Pendientes y En curso del empleado autenticado; excluye Realizadas/Canceladas. No depende de la página del listado. Volver al inicio consulta datos actuales; fallo de conexión no es cero válido.

Tareas asignadas muestra tipo, código, dirección, fecha e identificadores. Filtro inicial **Sin realizar** (Pendientes/En curso, incluso con trabajo registrado); **Realizadas**, solo finalizadas; **Todas**, todos los estados identificados. Páginas de diez, total y Anterior/Siguiente. Volver del detalle conserva filtro/página; si la página desapareció tras actualizar, se ajusta. Sin coincidencias se informa sin mostrar datos de otra sesión.

El detalle muestra datos disponibles reales: estado, tipo, cliente, baño, fecha, descripción/origen de la solicitud y observaciones existentes. Mantenimiento sin servicio puede carecer de cliente/dirección: no se inventan. Traslado muestra **Dirección de origen y Dirección de destino**, antes y después del cierre; usa referencias históricas de la tarea, no la ubicación actual posterior.

## Cumplimiento en dos pasos

1. **Registrar Trabajo:** persiste autor y fecha/hora del servidor; confirma solo tras guardar. Conserva estado. No descuenta contador ni completa observaciones o papel ficticios.
2. **Actualizar Estado:** habilitado con registro persistido; finaliza como Realizada, etiqueta visible Realizado en verde. Reabrir/reiniciar recupera ambos pasos. Un registro anterior permite finalizar después de iniciar sesión otra vez.

La API exige empleado activo y tarea propia en consulta/escritura. Revalida cancelación, finalización y reasignación; repeticiones no duplican registro/efectos. No intentar finalizar una tarea de otro empleado con un ID conocido.

Entrega actualiza servicio; traslado activa destino; retiro cierra servicio tras revisión administrativa. Si retiro/traslado está bloqueado, el detalle identifica trabajos o revisión pendiente: volver al administrador para resolverlos explícitamente. No completar una limpieza suponiendo que equivale a retirar el baño.

Finalizar mantenimiento deja de computar su bloqueo, pero conserva condición física hasta confirmación administrativa; otros bloqueos pueden seguir vigentes. Solicitudes se resuelven al completar todos los trabajos requeridos, no por registrar ni por cancelar una tarea.

## Prueba manual por grupos

1. Programar tarea nueva para hoy desde administración; ingresar empleado, comprobar contador, listado y detalle. Registrar sin finalizar y comprobar que sigue Sin realizar/contador.
2. Reabrir detalle o sesión: recuperar registro; finalizar y volver al inicio, comprobar descuento. Consultar Realizadas; volver conservando filtro/página.
3. Para traslado, verificar origen/destino; para retiro, confirmar IDs de tarea/servicio/solicitud y resolver bloqueantes desde administración. Comprobar resultado como cliente.

Captura de nuevas observaciones y reposición de papel sigue sin interfaz definida/implementada; mostrar campos existentes no equivale a poder cargarlos. Resultados aprobados y límites de plataforma: [verificaciones](verificaciones.md).
