using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class ClienteConfiguracion : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("CLIENTES");
        b.HasKey(x => x.IdCliente);
        b.Property(x => x.RazonSocial).HasMaxLength(200);
        b.Property(x => x.Cuit).HasMaxLength(20);
        b.Property(x => x.DireccionPrincipal).HasMaxLength(300);
        b.Property(x => x.Observaciones).HasMaxLength(2000);
        b.HasIndex(x => x.IdUsuario).IsUnique();
    }
}
