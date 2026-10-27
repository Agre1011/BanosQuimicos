using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class TipoBanoConfiguracion : IEntityTypeConfiguration<TipoBano>
{
    public void Configure(EntityTypeBuilder<TipoBano> b)
    {
        b.ToTable("TIPOS_BANO");
        b.HasKey(x => x.IdTipoBano);
        b.Property(x => x.NombreTipo).HasMaxLength(30).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(500);
        b.HasIndex(x => x.NombreTipo).IsUnique();
        b.HasData(
            new TipoBano { IdTipoBano = 1, NombreTipo = "Obra" },
            new TipoBano { IdTipoBano = 2, NombreTipo = "Evento" });
    }
}
