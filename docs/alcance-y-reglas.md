# Alcance y reglas confirmadas

Referencia vigente: originales más decisiones posteriores expresas del estudiante. La [cronología](historico/plan-trabajo.md) conserva la evolución; una propuesta histórica no es una decisión vigente. Sin pagos, facturación, login externo ni recuperación de contraseña.

## Cuentas y permisos

Registro público: usuario y perfil cliente juntos, Cliente/Activa definidos por la API. Correo único sin distinguir mayúsculas y sin espacios exteriores; sin nombre de usuario adicional. Nombre, apellido, correo, teléfono y contraseña obligatorios; razón social, CUIT y dirección principal opcionales. Teléfono/CUIT son texto; CUIT no es único.

Estado y rol son independientes. Cliente ↔ Empleado conserva usuario, contraseña, perfil e historial y no activa/reactiva. Antiguas Pendientes solo ven espera, consulta de estado y cierre de sesión; pueden activarse como Cliente o Empleado. Suspensión exige motivo; reactivar Suspendida conserva rol. Baja lógica definitiva desde la app, sin borrar registros.

Se impide cambiar el propio estado administrativo o dejar al sistema sin administradores activos. Para suspender/bajar bloquean tareas Pendientes/En curso o registro sin finalizar, servicios vigentes y solicitudes Pendientes/Aprobadas. Empleado → Cliente comprueba trabajos; Cliente → Empleado, servicios/solicitudes abiertos. No hay cancelación automática. Responsable habitual sin habilitación se conserva identificado, pero no recibe nuevas tareas.

La API comprueba estado/rol actuales y propiedad de registros, también al consultar directamente por ID. Ver [autenticación](autenticacion-api.md).

## Baños, servicios y tareas

- Tipo físico y uso del servicio son distintos (Obra/Evento); se permite baño de obra para evento. Las cantidades ilustrativas no limitan inventario.
- Un servicio por baño. Pedidos de varios baños generan varios servicios. Otro baño se pide con **Solicitar un nuevo servicio**, incluso en la misma dirección. Sin agrupación, herencia ni adicionales vinculados; el tipo histórico del catálogo se conserva sin ofrecerlo.
- Frecuencia: referencia de limpiezas por semana de cada servicio. Administración programa cada trabajo por fecha; sin generación automática semanal ni suma de frecuencias entre baños.
- Responsable habitual y empleado puntual de una tarea son referencias distintas. Asignar una tarea no cambia al habitual.
- Condición Apto/No apto, ocupación y baja de inventario son independientes. Asignación vigente ocupa el baño antes de entregarlo; sin reservas futuras superpuestas.
- Disponible para asignar: sin baja, Apto, sin servicio vigente ni mantenimiento bloqueante Pendiente/En curso. Limpieza pendiente no bloquea por sí sola. No apto no cierra ni libera servicio existente.
- Alta: solo tipo, código automático único no reutilizable, Apto inicial, sin servicio/responsable. Baja lógica impedida por servicio vigente o tareas activas; conserva historial. No apto no sustituye la baja.
- Mantenimiento puede no tener servicio; siempre tiene baño. Si referencia servicio, debe corresponder al mismo baño. Finalizar/cancelar elimina su bloqueo, pero no cambia automáticamente condición a Apto; lo confirma administración.
- Tareas Pendientes pueden no tener empleado; para avanzar exigen empleado activo. Solo administración asigna. Registrar Trabajo y finalizar son pasos persistidos separados; registrar no modifica estado ni inventa observaciones/papel.

## Solicitudes y cierre

Solicitudes: Pendiente, Aprobada, Rechazada, Resuelta, Cancelada. Tareas: Pendiente, En curso, Realizada, Cancelada. Catálogos distinguen ámbitos; tener un estado no implica ofrecer todas sus transiciones en la app.

Aprobar/asignar guarda solicitud Aprobada y tarea Pendiente juntas; no resuelve hasta completar los trabajos requeridos. Cancelar tarea no resuelve automáticamente. Reclamo sin trabajo permite respuesta administrativa obligatoria y cierre sin tareas activas ni trabajo ficticio.

Servicio nuevo: cliente indica dirección, cantidad, tipo, uso y fecha deseada. Administrador selecciona cantidad exacta de baños disponibles compatibles, frecuencia común, empleado y fecha común de entrega. Solicitar no crea servicios; asignar no entrega. Entregas parciales mantienen Aprobada.

Baja: solicitar/programar retiro no libera la unidad. Completar retiro cierra servicio y resuelve solicitud cuando corresponde. Administración revisa trabajos: decide explícitamente qué Pendientes sin registro cancelar o conservar/completar. En curso/con registro deben resolverse. Mantenimientos bloqueantes se conservan y pueden bloquear disponibilidad posterior. Retiro administrativo reutiliza baja abierta elegida; si no existe, no inventa solicitud. Cancelación antes de entrega libera sin retiro ficticio; no implica una acción general de cancelación implementada.

Traslado solicitado: ubicación cambia al finalizar la tarea. Informado: fecha/hora efectiva no futura y validación administrativa, sin tarea física. Se conserva ubicación anterior y fecha efectiva separada de registro/validación. Administración decide qué tareas Pendientes sin registro pasan al destino/conservan origen. En curso/con registro bloquean; Realizadas/Canceladas no se reescriben. Una sola operación abierta traslado/baja por servicio, incluyendo tareas activas administrativas.

## Diferencias con los originales

Originales conservados: [PDF de siete páginas](<Sistema de Gestión Móvil para Empresa de Baños Químicos (3).pdf>), [DER](<Imagen de ChatGPT 27 oct 2026, 09_11_39 a.m.png>) y [UML](<Imagen de ChatGPT 27 oct 2026, 09_11_49 a.m.png>). La revisión inicial registró texto y revisión visual de las siete páginas; textos pequeños/símbolos no permiten afirmar cada detalle. No se dispone del Figma editable.

Actualizar manualmente en los originales editables:

- PDF páginas 1–2 y UML página 4/PNG: sustituir adicional vinculado/caso 8 por nuevo servicio independiente; conservar caso 9. Figma cliente página 7: sin agrupaciones ni herencia.
- Login: correo en vez de nombre de usuario; registro activo sin espera normal. Mantener espera para antiguas Pendientes.
- DER: reflejar las extensiones descritas en [base de datos](base-datos.md), conservando 12 tablas.
- Figma páginas 5–7: adaptaciones autorizadas de Gestionar cuentas/roles/estados, cumplimiento en dos pasos en detalle, selección de baños/fecha, revisión de trabajos/ubicación, respuesta administrativa, inventario/mantenimiento/filtros, uso separado del tipo, paginación y logo solo en acceso/tres inicios. Retirada de actualización de cuenta del inicio administrador.

Estas autorizaciones no habilitan otras pantallas sin referencia. Pendientes y limitaciones en el [plan](plan-trabajo.md).
