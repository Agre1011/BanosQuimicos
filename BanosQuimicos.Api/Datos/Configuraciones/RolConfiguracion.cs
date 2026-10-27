using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class RolConfiguracion : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> b)
    {
        b.ToTable("ROLES");
        b.HasKey(x => x.IdRol);
        b.Property(x => x.NombreRol).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.NombreRol).IsUnique();
        b.HasData(
            new Rol { IdRol = 1, NombreRol = "Administrador" },
            new Rol { IdRol = 2, NombreRol = "Empleado" },
            new Rol { IdRol = 3, NombreRol = "Cliente" });
    }
}
