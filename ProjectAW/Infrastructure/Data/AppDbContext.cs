using Microsoft.EntityFrameworkCore;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Categorias.Entities;
using ProjectAW.Modules.Maestros.Clientes.Entities;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Maestros.Productos.Entities;
using ProjectAW.Modules.Maestros.Proveedores.Entities;
using ProjectAW.Modules.Maestros.Roles.Entities;
using ProjectAW.Modules.Maestros.Ubicaciones.Entities;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;

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
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<CabeceraGuia> CabeceraGuia => Set<CabeceraGuia>();
        public DbSet<DetalleGuia> DetalleGuia => Set<DetalleGuia>();
        public DbSet<TipoOperacionGuia> TiposOperacionesGuia => Set<TipoOperacionGuia>();
        public DbSet<EstadoGuia> EstadosGuia => Set<EstadoGuia>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);


            // AppDbContext.OnModelCreating
            modelBuilder.HasSequence<int>("SeqGuiaEntrada");
            modelBuilder.HasSequence<int>("SeqGuiaSalida");
            modelBuilder.HasSequence<int>("SeqGuiaTraslado");

        }
    }
}
