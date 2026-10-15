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
using ProjectAW.Modules.Seguridad.Security.HttpAuth;
using Microsoft.AspNetCore.Identity;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;
using ProjectAW.Modules.Seguridad.Security.Repositories;
using ProjectAW.Modules.Seguridad.Security.Services;
using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Maestros.Roles.Repositories;
using ProjectAW.Modules.Maestros.Roles.Services;
using ProjectAW.Modules.Maestros.TipoDocumentos.Repositories;
using ProjectAW.Modules.Maestros.TipoDocumentos.Services;
using ProjectAW.Infrastructure.Errors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddControllersWithViews();
// MVC
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationExpanders.Add(new ModuleViewLocationExpander());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "La solicitud contiene datos no válidos.",
                Detail = "Revisa los campos indicados y vuelve a intentarlo.",
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["mensaje"] = problem.Detail;
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            return new BadRequestObjectResult(problem);
        };
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Acceso desde la PWA con cookie: los orígenes se configuran por entorno/cliente.
var pwaOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("PwaLogin", policy =>
    policy.WithOrigins(pwaOrigins)
        .WithMethods("POST")
        .WithHeaders("Content-Type")
        .AllowCredentials()));
builder.Services.AddCors(options => options.AddPolicy("PwaRead", policy =>
    policy.WithOrigins(pwaOrigins)
        .WithMethods("GET")
        .WithHeaders("Accept")
        .AllowCredentials()));

// Respuestas de error RFC 7807 y conversión centralizada de excepciones.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

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
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<IRolService, RolService>();
builder.Services.AddScoped<ITipoDocumentoRepository, TipoDocumentoRepository>();
builder.Services.AddScoped<ITipoDocumentoService, TipoDocumentoService>();

// builder.Services.AddScoped
// token api 
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ProjectAW.Auth";
        options.Cookie.HttpOnly = true;                     // Javascript no puede leer la cookie
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;                   // Se renueva mientras el usuario este activo
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccesoDenegado";

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            else
                context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });




var app = builder.Build();

// Limita el formato centralizado al contrato HTTP de las APIs.
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    api =>
    {
        api.UseExceptionHandler();
        api.UseStatusCodePages();
    });

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
app.UseCors();

app.UseAuthentication(); // api
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers().RequireAuthorization(); // si no quieres el token comenta esta linea unicamente 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
