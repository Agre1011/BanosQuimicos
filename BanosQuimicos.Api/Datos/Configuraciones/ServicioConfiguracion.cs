using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class ServicioConfiguracion : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> b)
    {
        b.ToTable("SERVICIOS", t =>
        {
            t.HasCheckConstraint("CK_SERVICIOS_TipoUso", "[TipoUso] IN (N'Obra', N'Evento')");
            t.HasCheckConstraint("CK_SERVICIOS_Frecuencia", "[CantidadLimpiezasSemana] >= 0");
            t.HasCheckConstraint("CK_SERVICIOS_Ciclo", "([EstadoServicio] = N'Pendiente de entrega' AND [FechaEntregaReal] IS NULL AND [FechaRetiroReal] IS NULL AND [FechaFinalizacion] IS NULL AND [FechaCancelacion] IS NULL) OR ([EstadoServicio] = N'Entregado' AND [FechaEntregaReal] IS NOT NULL AND [FechaRetiroReal] IS NULL AND [FechaFinalizacion] IS NULL AND [FechaCancelacion] IS NULL) OR ([EstadoServicio] = N'Finalizado' AND [FechaEntregaReal] IS NOT NULL AND [FechaRetiroReal] IS NOT NULL AND [FechaFinalizacion] IS NOT NULL AND [FechaCancelacion] IS NULL) OR ([EstadoServicio] = N'Cancelado' AND [FechaEntregaReal] IS NULL AND [FechaRetiroReal] IS NULL AND [FechaFinalizacion] IS NULL AND [FechaCancelacion] IS NOT NULL)");
            t.HasCheckConstraint("CK_SERVICIOS_Fechas", "([FechaEntregaReal] IS NULL OR [FechaEntregaReal] >= [FechaAsignacion]) AND ([FechaRetiroReal] IS NULL OR [FechaRetiroReal] >= [FechaEntregaReal]) AND ([FechaFinalizacion] IS NULL OR [FechaFinalizacion] >= [FechaRetiroReal]) AND ([FechaCancelacion] IS NULL OR [FechaCancelacion] >= [FechaAsignacion])");
        });
        b.HasKey(x => x.IdServicio);
        b.Property(x => x.TipoUso).HasMaxLength(10).IsRequired();
        b.Property(x => x.EstadoServicio).HasMaxLength(30).IsRequired();
        b.Property(x => x.Observaciones).HasMaxLength(2000);
        // Los dos primeros estados mantienen la unidad asignada, aun antes de la entrega.
        b.HasIndex(x => x.IdBano).IsUnique()
            .HasDatabaseName("UX_SERVICIOS_BanoAsignado")
            .HasFilter("[EstadoServicio] IN (N'Pendiente de entrega', N'Entregado')");
        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.IdCliente);
        b.HasOne(x => x.Bano).WithMany().HasForeignKey(x => x.IdBano);
        b.HasOne(x => x.SolicitudOrigen).WithMany().HasForeignKey(x => x.IdSolicitudOrigen);
    }
}
