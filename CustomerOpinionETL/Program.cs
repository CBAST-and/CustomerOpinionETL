using CustomerOpinionETL.API.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// CONFIGURAR SERVICIOS
// =============================================

// Controllers
builder.Services.AddControllers();

// CORS (permitir que el ETL Worker acceda a la API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowETL", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Customer Opinion Social Media API",
        Version = "v1",
        Description = "Mock API que simula comentarios de redes sociales (Instagram, Twitter, Facebook) para el sistema ETL de análisis de opiniones de clientes.",
        Contact = new OpenApiContact
        {
            Name = "ETL Team",
            Email = "etl@customeropinion.com"
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

// Registrar el servicio de datos
builder.Services.AddSingleton<ISocialMediaDataService, SocialMediaDataService>();

// Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// =============================================
// CONFIGURAR MIDDLEWARE
// =============================================

var app = builder.Build();

// Swagger UI (disponible en todos los entornos para testing)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Social Media API v1");
    c.RoutePrefix = "swagger";
});

// CORS
app.UseCors("AllowETL");

// HTTPS Redirection
app.UseHttpsRedirection();

// Authorization
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Página de bienvenida en la raíz
app.MapGet("/", () => Results.Redirect("/swagger"));

// =============================================
// INICIAR API
// =============================================

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("|-------------------------------------------------------|");
logger.LogInformation("|   SOCIAL MEDIA API - CUSTOMER OPINION ETL SYSTEM      |");
logger.LogInformation("|_______________________________________________________|");
logger.LogInformation("");
logger.LogInformation(" API Starting...");
logger.LogInformation(" CSV Data Path: {Path}", builder.Configuration["DataSource:CsvFilePath"]);
logger.LogInformation(" Swagger UI: http://localhost:5000/swagger");
logger.LogInformation("");

app.Run();

logger.LogInformation("API Stopped");