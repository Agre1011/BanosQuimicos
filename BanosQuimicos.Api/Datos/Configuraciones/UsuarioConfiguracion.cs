using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("USUARIOS", t =>
        {
            t.HasCheckConstraint("CK_USUARIOS_Email", "DATALENGTH([Email]) = DATALENGTH(LTRIM(RTRIM([Email]))) AND LEN([Email]) > 0");
            t.HasCheckConstraint("CK_USUARIOS_DatosObligatorios", "LEN(LTRIM(RTRIM([Nombre]))) > 0 AND LEN(LTRIM(RTRIM([Apellido]))) > 0 AND LEN(LTRIM(RTRIM([Telefono]))) > 0 AND LEN([ContrasenaHash]) > 0");
            t.HasCheckConstraint("CK_USUARIOS_EstadoCuenta", "[EstadoCuenta] IN (N'Pendiente', N'Activa', N'Suspendida', N'BajaLogica')");
        });
        b.HasKey(x => x.IdUsuario);
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Apellido).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254).IsRequired().UseCollation("Latin1_General_100_CI_AS");
        b.Property(x => x.Telefono).HasMaxLength(40).IsRequired();
        b.Property(x => x.ContrasenaHash).HasMaxLength(1000).IsRequired();
        b.Property(x => x.EstadoCuenta).HasMaxLength(20).IsRequired();
        b.Property(x => x.MotivoSuspension).HasMaxLength(1000);
        b.Property(x => x.EstadoCuenta).HasDefaultValue("Pendiente");
        b.HasIndex(x => x.Email).IsUnique();
        b.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.IdRol);
        b.HasOne(x => x.Cliente).WithOne(x => x.Usuario).HasForeignKey<Cliente>(x => x.IdUsuario);
    }
}
