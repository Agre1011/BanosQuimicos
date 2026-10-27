using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class EstadoBanoConfiguracion : IEntityTypeConfiguration<EstadoBano>
{
    public void Configure(EntityTypeBuilder<EstadoBano> b)
    {
        b.ToTable("ESTADOS_BANO");
        b.HasKey(x => x.IdEstadoBano);
        b.Property(x => x.NombreEstado).HasMaxLength(30).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(500);
        b.HasIndex(x => x.NombreEstado).IsUnique();
        b.HasData(
            new EstadoBano { IdEstadoBano = 1, NombreEstado = "Apto" },
            new EstadoBano { IdEstadoBano = 2, NombreEstado = "No apto" });
    }
}
