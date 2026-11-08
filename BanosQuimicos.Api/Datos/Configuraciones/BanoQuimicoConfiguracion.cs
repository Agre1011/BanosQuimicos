using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class BanoQuimicoConfiguracion : IEntityTypeConfiguration<BanoQuimico>
{
    public void Configure(EntityTypeBuilder<BanoQuimico> b)
    {
        b.ToTable("BANOS_QUIMICOS", t =>
        {
            t.HasCheckConstraint("CK_BANOS_QUIMICOS_Codigo", "DATALENGTH([CodigoInterno]) = DATALENGTH(LTRIM(RTRIM([CodigoInterno]))) AND LEN([CodigoInterno]) > 0");
            t.HasCheckConstraint("CK_BANOS_QUIMICOS_FechaBaja", "[FechaBaja] IS NULL OR [FechaBaja] >= [FechaAlta]");
        });
        b.HasKey(x => x.IdBano);
        b.Property(x => x.CodigoInterno).HasMaxLength(30).IsRequired().UseCollation("Latin1_General_100_CI_AS");
        b.Property(x => x.Observaciones).HasMaxLength(2000);
        b.HasIndex(x => x.CodigoInterno).IsUnique();
        b.HasIndex(x => x.ClaveAlta).IsUnique().HasFilter("[ClaveAlta] IS NOT NULL");
        b.HasOne(x => x.TipoBano).WithMany().HasForeignKey(x => x.IdTipoBano);
        b.HasOne(x => x.EstadoBano).WithMany().HasForeignKey(x => x.IdEstadoBano);
        b.HasOne(x => x.ResponsableLimpieza).WithMany().HasForeignKey(x => x.IdResponsableLimpieza);
    }
}
