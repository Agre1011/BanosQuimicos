using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanosQuimicos.Api.Datos.Configuraciones;

public class EstadoOperacionConfiguracion : IEntityTypeConfiguration<EstadoOperacion>
{
    public void Configure(EntityTypeBuilder<EstadoOperacion> b)
    {
        b.ToTable("ESTADOS_OPERACION", t =>
        {
            t.HasCheckConstraint("CK_ESTADOS_OPERACION_Ambito", "[Ambito] IN (N'Solicitud', N'Tarea')");
        });
        b.HasKey(x => x.IdEstadoOperacion);
        b.Property(x => x.NombreEstado).HasMaxLength(30).IsRequired();
        b.Property(x => x.Ambito).HasMaxLength(10).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(500);
        b.HasIndex(x => new { x.Ambito, x.NombreEstado }).IsUnique();
        b.HasData(
            new EstadoOperacion { IdEstadoOperacion = 1, NombreEstado = "Pendiente", Ambito = "Solicitud" },
            new EstadoOperacion { IdEstadoOperacion = 2, NombreEstado = "Aprobada", Ambito = "Solicitud" },
            new EstadoOperacion { IdEstadoOperacion = 3, NombreEstado = "Rechazada", Ambito = "Solicitud" },
            new EstadoOperacion { IdEstadoOperacion = 4, NombreEstado = "Resuelta", Ambito = "Solicitud" },
            new EstadoOperacion { IdEstadoOperacion = 5, NombreEstado = "Cancelada", Ambito = "Solicitud" },
            new EstadoOperacion { IdEstadoOperacion = 6, NombreEstado = "Pendiente", Ambito = "Tarea" },
            new EstadoOperacion { IdEstadoOperacion = 7, NombreEstado = "En curso", Ambito = "Tarea" },
            new EstadoOperacion { IdEstadoOperacion = 8, NombreEstado = "Realizada", Ambito = "Tarea" },
            new EstadoOperacion { IdEstadoOperacion = 9, NombreEstado = "Cancelada", Ambito = "Tarea" });
    }
}
