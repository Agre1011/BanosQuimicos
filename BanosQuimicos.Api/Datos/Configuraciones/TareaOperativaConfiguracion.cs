using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class TareaOperativaConfiguracion : IEntityTypeConfiguration<TareaOperativa>
{
    public void Configure(EntityTypeBuilder<TareaOperativa> b)
    {
        b.ToTable("TAREAS_OPERATIVAS", t =>
        {
            // IDs estables del catálogo inicial: 11 = mantenimiento; 7/8 = en curso/realizada.
            // Cambiarlos requiere una migración que actualice también estas restricciones.
            t.HasCheckConstraint("CK_TAREAS_OPERATIVAS_Bloqueo", "[BloqueaAsignacion] = 0 OR [IdTipoOperacion] = 11");
            t.HasCheckConstraint("CK_TAREAS_OPERATIVAS_Servicio", "[IdServicio] IS NOT NULL OR [IdTipoOperacion] = 11");
            t.HasCheckConstraint("CK_TAREAS_OPERATIVAS_Empleado", "[IdEstadoOperacion] NOT IN (7, 8) OR [IdEmpleado] IS NOT NULL");
            t.HasCheckConstraint("CK_TAREAS_OPERATIVAS_RegistroTrabajo", "([FechaTrabajoRegistrado] IS NULL AND [IdEmpleadoRegistroTrabajo] IS NULL) OR ([FechaTrabajoRegistrado] IS NOT NULL AND [IdEmpleadoRegistroTrabajo] IS NOT NULL)");
        });
        b.HasKey(x => x.IdTarea);
        b.Property(x => x.ObservacionesEmpleado).HasMaxLength(2000);
        b.HasOne(x => x.Bano).WithMany().HasForeignKey(x => x.IdBano);
        b.HasOne(x => x.TipoOperacion).WithMany().HasForeignKey(x => x.IdTipoOperacion);
        b.HasOne(x => x.EstadoOperacion).WithMany().HasForeignKey(x => x.IdEstadoOperacion);
        b.HasOne(x => x.Servicio).WithMany().HasForeignKey(x => x.IdServicio);
        b.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.IdEmpleado);
        b.HasOne(x => x.EmpleadoRegistroTrabajo).WithMany().HasForeignKey(x => x.IdEmpleadoRegistroTrabajo);
        b.HasOne(x => x.Solicitud).WithMany().HasForeignKey(x => x.IdSolicitud);
        b.HasOne(x => x.Ubicacion).WithMany().HasForeignKey(x => x.IdUbicacion);
        b.HasOne(x => x.UbicacionDestino).WithMany().HasForeignKey(x => x.IdUbicacionDestino);
        b.HasIndex(x => new { x.IdBano, x.BloqueaAsignacion, x.IdEstadoOperacion });
    }
}
