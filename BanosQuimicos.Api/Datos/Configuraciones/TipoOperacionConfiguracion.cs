using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class TipoOperacionConfiguracion : IEntityTypeConfiguration<TipoOperacion>
{
    public void Configure(EntityTypeBuilder<TipoOperacion> b)
    {
        b.ToTable("TIPOS_OPERACION", t =>
        {
            t.HasCheckConstraint("CK_TIPOS_OPERACION_Aplicacion", "[AplicaASolicitudes] = 1 OR [AplicaATareas] = 1");
        });
        b.HasKey(x => x.IdTipoOperacion);
        b.Property(x => x.NombreOperacion).HasMaxLength(50).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(500);
        b.HasIndex(x => x.NombreOperacion).IsUnique();
        // Los identificadores de los catálogos son estables entre migraciones.
        b.HasData(
            new TipoOperacion { IdTipoOperacion = 1, NombreOperacion = "Nuevo servicio", AplicaASolicitudes = true },
            new TipoOperacion { IdTipoOperacion = 2, NombreOperacion = "Baño adicional", AplicaASolicitudes = true },
            new TipoOperacion { IdTipoOperacion = 3, NombreOperacion = "Limpieza extra", AplicaASolicitudes = true, AplicaATareas = true },
            new TipoOperacion { IdTipoOperacion = 4, NombreOperacion = "Cambio de dirección", AplicaASolicitudes = true },
            new TipoOperacion { IdTipoOperacion = 5, NombreOperacion = "Reclamo", AplicaASolicitudes = true },
            new TipoOperacion { IdTipoOperacion = 6, NombreOperacion = "Baja", AplicaASolicitudes = true },
            new TipoOperacion { IdTipoOperacion = 7, NombreOperacion = "Entrega", AplicaATareas = true },
            new TipoOperacion { IdTipoOperacion = 8, NombreOperacion = "Retiro", AplicaATareas = true },
            new TipoOperacion { IdTipoOperacion = 9, NombreOperacion = "Limpieza semanal", AplicaATareas = true },
            new TipoOperacion { IdTipoOperacion = 10, NombreOperacion = "Traslado", AplicaATareas = true },
            new TipoOperacion { IdTipoOperacion = 11, NombreOperacion = "Mantenimiento", AplicaATareas = true });
    }
}
