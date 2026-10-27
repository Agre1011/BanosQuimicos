using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class UbicacionServicioConfiguracion : IEntityTypeConfiguration<UbicacionServicio>
{
    public void Configure(EntityTypeBuilder<UbicacionServicio> b)
    {
        b.ToTable("UBICACIONES_SERVICIO", t =>
        {
            t.HasCheckConstraint("CK_UBICACIONES_SERVICIO_Direccion", "LEN(LTRIM(RTRIM([Direccion]))) > 0");
            t.HasCheckConstraint("CK_UBICACIONES_SERVICIO_Intervalo", "([FechaHasta] IS NULL OR ([FechaDesde] IS NOT NULL AND [FechaHasta] >= [FechaDesde])) AND ([EsActual] = 0 OR ([FechaDesde] IS NOT NULL AND [FechaHasta] IS NULL))");
        });
        b.HasKey(x => x.IdUbicacion);
        b.Property(x => x.Direccion).HasMaxLength(300).IsRequired();
        b.Property(x => x.Localidad).HasMaxLength(100);
        b.Property(x => x.Referencia).HasMaxLength(500);
        b.HasOne(x => x.Servicio).WithMany().HasForeignKey(x => x.IdServicio);
        b.HasIndex(x => x.IdServicio).IsUnique().HasFilter("[EsActual] = 1").HasDatabaseName("UX_UBICACIONES_SERVICIO_Actual");
    }
}
