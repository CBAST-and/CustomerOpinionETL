using CustomerOpinionETL.Application.Interfaces;
using CustomerOpinionETL.Application.Interfaces.Extraction;
using CustomerOpinionETL.Application.Interfaces.Transformation;
using CustomerOpinionETL.Application.UseCases;
using CustomerOpinionETL.Infrastructure.Extractors;
using CustomerOpinionETL.Infrastructure.Repositories;
using CustomerOpinionETL.Infrastructure.Services;
using CustomerOpinionETL.Infrastructure.Transformers;
using CustomerOpinionETL.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

// =============================================
// Configurar Serilog para logging
// =============================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/etl-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("=== Starting ETL Worker Service ===");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            var configuration = hostContext.Configuration;

            // =============================================
            // CONFIGURACIONES
            // =============================================

            // CSV Extractor Configuration
            services.Configure<CsvExtractorConfiguration>(
                configuration.GetSection("ETL:DataSources:CSV"));

            // Database Extractor Configuration
            services.Configure<DatabaseExtractorConfiguration>(
                configuration.GetSection("ETL:DataSources:Database"));

            // API Extractor Configuration
            services.Configure<ApiExtractorConfiguration>(
                configuration.GetSection("ETL:DataSources:API"));

            // =============================================
            // HTTP CLIENT (para API Extractor)
            // =============================================

            services.AddHttpClient("SocialMediaAPI", client =>
            {
                var apiConfig = configuration.GetSection("ETL:DataSources:API");
                var baseUrl = apiConfig["BaseUrl"];

                if (!string.IsNullOrEmpty(baseUrl))
                {
                    client.BaseAddress = new Uri(baseUrl);
                }

                client.Timeout = TimeSpan.FromSeconds(
                    apiConfig.GetValue<int>("TimeoutSeconds", 30));
            });

            // =============================================
            // EXTRACTORS (3 fuentes de datos)
            // =============================================

            services.AddScoped<ICsvExtractor, CsvExtractor>();
            services.AddScoped<IDatabaseExtractor, DatabaseExtractor>();
            services.AddScoped<IApiExtractor, ApiExtractor>();

            // =============================================
            // TRANSFORMERS Y SERVICES
            // =============================================

            services.AddScoped<ISentimentAnalyzer, SentimentAnalyzerService>();
            services.AddScoped<IOpinionTransformer, OpinionTransformer>();

            // =============================================
            // REPOSITORIES (Unit of Work)
            // =============================================

            services.AddScoped<IUnitOfWork>(provider =>
            {
                var connectionString = configuration.GetConnectionString("AnalyticsDb")
                    ?? throw new InvalidOperationException("Connection string 'AnalyticsDb' not found");

                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                return new UnitOfWork(connectionString, loggerFactory);
            });

            // =============================================
            // USE CASES
            // =============================================

            services.AddScoped<ETLOrchestrator>();
            services.AddScoped<LoadDimensionsUseCase>();

            // =============================================
            // WORKER SERVICE
            // =============================================

            services.AddHostedService<ETLWorkerService>();

            // =============================================
            // CONFIGURACIÓN ADICIONAL
            // =============================================

            // Logging configuration
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            });
        })
        .Build();

    // Ejecutar el host
    await host.RunAsync();

    Log.Information("=== ETL Worker Service stopped cleanly ===");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "ETL Worker Service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
