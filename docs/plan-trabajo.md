# Plan de trabajo vigente

Actualizado el 08/11/2026. Reglas en [alcance](alcance-y-reglas.md), evidencia en [verificaciones](verificaciones.md) y evolución completa en el [plan histórico](historico/plan-trabajo.md).

## Estado

Integrados los recorridos de cuentas, inventario, programación manual, tareas, servicios y solicitudes de cliente/administrador, traslados, retiros y respuesta administrativa a reclamos. Límites y recorridos: [administrador](administrador.md), [empleado](empleado.md), [cliente](cliente.md). Cerrar un bloque no implica haber completado cada función del PDF.

Registro vigente Cliente/Activa, sin aprobación. Adicionales vinculados descartados: otro baño se pide como nuevo servicio independiente. Las referencias antiguas contrarias son históricas.

## Orden de cierre

1. **Limpieza (chore/limpieza-proyecto), cerrada para revisión:** retirados recursos sin uso/comentarios de plantilla y consolidada documentación. Enlaces válidos, originales/configuración conservados, diff sin lógica de negocio y compilación Windows/Android correcta. Resultados y límites en [verificaciones](verificaciones.md#limpieza-del-proyecto). Esperar revisión antes del siguiente bloque; sin commit/push/merge.
2. **Pendientes funcionales:** la captura de observaciones del empleado y reposición de papel no forma parte del cumplimiento actual. Requiere definir el control visual antes de implementarlo; no inventar valores ni darla por completa. La reasignación de tareas existentes tampoco tiene un recorrido general desde la app: confirmar su alcance si se necesita para resolver bloqueos de cuentas. No agregar rechazo/cancelación de solicitudes ni borrados por un icono sin regla confirmada.
3. **Integral Android contra API local:** los tres roles conectados, permisos, estados, filtros, paginación, teclado, navegación y recuperación. Registrar cada resultado; USB/registro/login aprobados no cubren la integral.
4. **Correcciones y sesión:** vencimiento real del token y alternancia local/hosting en dispositivo, sin reutilizar sesiones del otro servidor. No anticipar aprobación.
5. **Migraciones y republicación:** revisar historial real, respaldo y script incremental; después comprobar recorridos por HTTPS. Lista/procedimiento en [publicación](publicacion.md).

No publicar antes de integral/correcciones. Ningún paso de limpieza modifica datos ni aplica migraciones. No hacer commit, push o merge sin autorización.

## Diseño y documentación

Conservar PDF, DER, UML y capturas. El estudiante actualizará las [diferencias con los originales](alcance-y-reglas.md#diferencias-con-los-originales). Pedir referencia antes de crear una adaptación visual no autorizada.

La retirada de Actualizar estado de la cuenta solo del inicio administrador fue aprobada manualmente después del cierre histórico del bloque. Gestionar cuentas y las validaciones de la API se conservan.
