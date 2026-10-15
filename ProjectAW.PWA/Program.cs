var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

var defaultFiles = new DefaultFilesOptions();
defaultFiles.DefaultFileNames.Clear();
defaultFiles.DefaultFileNames.Add("modules/seguridad/login/index.html");
app.UseDefaultFiles(defaultFiles);
app.UseStaticFiles();

app.Run();
