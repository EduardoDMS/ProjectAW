using Microsoft.EntityFrameworkCore;
using ProjectAW.Modules.Maestros.Productos.Repositories;
using ProjectAW.Modules.Maestros.Productos.Services;
using ProjectAW.Infrastructure.MVC;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.Almacenes.Services;
using ProjectAW.Modules.Maestros.Almacenes.Repositories;
using ProjectAW.Modules.Maestros.Categorias.Services;
using ProjectAW.Modules.Maestros.Categorias.Repositories;
using ProjectAW.Modules.Maestros.Clientes.Repositories;
using ProjectAW.Modules.Maestros.Clientes.Services;
using ProjectAW.Modules.Maestros.Proveedores.Repositories;
using ProjectAW.Modules.Maestros.Proveedores.Services;
using ProjectAW.Modules.Maestros.Ubicaciones.Services;
using ProjectAW.Modules.Maestros.Ubicaciones.Repositories;
using ProjectAW.Modules.Seguridad.Usuarios.Repositories;
using ProjectAW.Modules.Seguridad.Usuarios.Services;
using ProjectAW.Modules.Guias.Repositories;
using ProjectAW.Modules.Guias.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddControllersWithViews();
// MVC
builder.Services.AddControllersWithViews().AddRazorOptions(options =>
{
    options.ViewLocationExpanders.Add(new ModuleViewLocationExpander());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DI
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IAlmacenService, AlmacenService>();
builder.Services.AddScoped<IAlmacenRepository, AlmacenRepository>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IUbicacionService, UbicacionService>();
builder.Services.AddScoped<IUbicacionRepository, UbicacionRepository>();
builder.Services.AddScoped<IGuiaRepository, GuiaRepository>();
builder.Services.AddScoped<IGuiaService, GuiaService>();

// builder.Services.AddScoped



var app = builder.Build();

// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

//app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
