using CustomerOpinionETL.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Registrar el servicio de datos
builder.Services.AddScoped<IDashboardDataService, DashboardDataService>();

// Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("|-------------------------------------------------------|");
logger.LogInformation("|   CUSTOMER OPINION DASHBOARD - ETL ANALYTICS          |");
logger.LogInformation("|-------------------------------------------------------|");
logger.LogInformation("Dashboard running at: {Urls}", builder.Configuration["Urls"] ?? "http://localhost:5100");

app.Run();