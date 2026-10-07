using Microsoft.EntityFrameworkCore;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Categorias.Entities;
using ProjectAW.Modules.Maestros.Clientes.Entities;
using ProjectAW.Modules.Maestros.Productos.Entities;
using ProjectAW.Modules.Maestros.Proveedores.Entities;
using ProjectAW.Modules.Maestros.Ubicaciones.Entities;

namespace ProjectAW.Infrastructure.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Almacen> Almacenes => Set<Almacen>();
        public DbSet<Ubicacion> Ubicaciones => Set<Ubicacion>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
