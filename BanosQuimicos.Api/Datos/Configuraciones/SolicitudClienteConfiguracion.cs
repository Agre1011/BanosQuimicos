using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class SolicitudClienteConfiguracion : IEntityTypeConfiguration<SolicitudCliente>
{
    public void Configure(EntityTypeBuilder<SolicitudCliente> b)
    {
        b.ToTable("SOLICITUDES_CLIENTE", t =>
        {
            t.HasCheckConstraint("CK_SOLICITUDES_CLIENTE_Cantidad", "[CantidadBanos] IS NULL OR [CantidadBanos] > 0");
            t.HasCheckConstraint("CK_SOLICITUDES_CLIENTE_Uso", "[TipoUsoSolicitado] IS NULL OR [TipoUsoSolicitado] IN (N'Obra', N'Evento')");
            t.HasCheckConstraint("CK_SOLICITUDES_CLIENTE_Modalidad", "[ModalidadCambioDireccion] IS NULL OR [ModalidadCambioDireccion] IN (N'Solicitado', N'Informado')");
        });
        b.HasKey(x => x.IdSolicitud);
        // Los pedidos anteriores sin clave se conservan; solo claves no nulas son únicas.
        b.HasIndex(x => x.ClaveEnvio).IsUnique().HasFilter("[ClaveEnvio] IS NOT NULL");
        b.Property(x => x.Descripcion).HasMaxLength(2000);
        b.Property(x => x.TipoUsoSolicitado).HasMaxLength(10);
        b.Property(x => x.DireccionSolicitada).HasMaxLength(300);
        b.Property(x => x.NuevaDireccion).HasMaxLength(300);
        b.Property(x => x.ModalidadCambioDireccion).HasMaxLength(20);
        b.Property(x => x.ObservacionesAdministrador).HasMaxLength(2000);
        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.IdCliente);
        b.HasOne(x => x.TipoOperacion).WithMany().HasForeignKey(x => x.IdTipoOperacion);
        b.HasOne(x => x.EstadoOperacion).WithMany().HasForeignKey(x => x.IdEstadoOperacion);
        b.HasOne(x => x.ServicioReferencia).WithMany().HasForeignKey(x => x.IdServicioReferencia);
        b.HasOne(x => x.TipoBanoSolicitado).WithMany().HasForeignKey(x => x.IdTipoBanoSolicitado);
    }
}
