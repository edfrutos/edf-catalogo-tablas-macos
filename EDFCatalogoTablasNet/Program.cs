using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using EDFCatalogoTablasNet.Services;
using EDFCatalogoTablasNet.Configuration;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

// Cargar appsettings.local.json (con credenciales sensibles)
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Configurar URLs específicas
builder.WebHost.UseUrls("http://localhost:5005", "https://localhost:7005");

// Agregar servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Agregar servicios Blazor Server y Razor Pages con antiforgery deshabilitado
builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
});
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

// Registrar CircuitHandler para mantener autenticación en Blazor Server
builder.Services.AddScoped<CircuitHandler, AuthCircuitHandler>();

// Configurar HttpClient para Blazor
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5005")
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "EDF Catálogo de Tablas API",
        Version = "v1",
        Description = "API para gestión de catálogos de tablas - Versión .NET 9.0",
        Contact = new()
        {
            Name = "EDF Proyectos",
            Email = "edefrutos@empresa.com"
        }
    });

    // Incluir comentarios XML para documentación
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configurar CORS para desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurar MongoDB
var mongoSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
if (mongoSettings == null)
{
    throw new Exception("MongoDB configuration is missing");
}
builder.Services.AddSingleton(mongoSettings);
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbContext>();

// Agregar HttpContextAccessor (necesario para sesiones)
builder.Services.AddHttpContextAccessor();

// Agregar servicios de sesión
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Registrar servicios de la aplicación usando MongoDB
builder.Services.AddScoped<ICatalogService, CatalogServiceMongo>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthServiceMongo>();
builder.Services.AddSingleton<AuthStateService>();
builder.Services.AddScoped<CircuitIdService>();

// Configurar AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Inicializar base de datos con datos de ejemplo
using (var scope = app.Services.CreateScope())
{
    var seeder = new DatabaseSeeder(
        scope.ServiceProvider.GetRequiredService<MongoDbContext>(),
        scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>()
    );
    await seeder.SeedAsync();
}

// Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EDF Catálogo API v1");
        c.RoutePrefix = "api-docs"; // Mover Swagger a /api-docs
    });
    app.UseCors("Development");
}

// Deshabilitar HTTPS redirection en desarrollo para facilitar las pruebas
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Configurar archivos estáticos y Blazor
app.UseStaticFiles();

// Configurar archivos estáticos para uploads
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();

// IMPORTANTE: Mapear endpoints específicos ANTES del fallback
// Endpoint de salud
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Port = "5005"
});

// Mapear controladores API
app.MapControllers();

// Mapear páginas Blazor
app.MapRazorPages();
app.MapBlazorHub();

// NOTA: MapFallbackToPage debe ser el ÚLTIMO para no interceptar rutas API
app.MapFallbackToPage("/_Host");

Console.WriteLine("🚀 Iniciando EDF Catálogo de Tablas (.NET 9.0)...");
Console.WriteLine($"🌍 Entorno: {app.Environment.EnvironmentName}");
Console.WriteLine("🌐 Interfaz Web Amigable: http://localhost:5005");
Console.WriteLine("🔒 HTTPS: https://localhost:7005");
Console.WriteLine("📖 Documentación API Técnica: http://localhost:5005/api-docs");
Console.WriteLine("⚡ API Health Check: http://localhost:5005/health");

app.Run();
