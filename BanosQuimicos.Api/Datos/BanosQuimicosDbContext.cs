using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Datos;

public class BanosQuimicosDbContext(DbContextOptions<BanosQuimicosDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<BanoQuimico> BanosQuimicos => Set<BanoQuimico>();
    public DbSet<TipoBano> TiposBano => Set<TipoBano>();
    public DbSet<EstadoBano> EstadosBano => Set<EstadoBano>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<UbicacionServicio> UbicacionesServicio => Set<UbicacionServicio>();
    public DbSet<SolicitudCliente> SolicitudesCliente => Set<SolicitudCliente>();
    public DbSet<TareaOperativa> TareasOperativas => Set<TareaOperativa>();
    public DbSet<TipoOperacion> TiposOperacion => Set<TipoOperacion>();
    public DbSet<EstadoOperacion> EstadosOperacion => Set<EstadoOperacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BanosQuimicosDbContext).Assembly);

        // Borrar un registro no debe arrastrar servicios, tareas ni ubicaciones históricas.
        foreach (var relacion in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            relacion.DeleteBehavior = DeleteBehavior.NoAction;
    }
}
