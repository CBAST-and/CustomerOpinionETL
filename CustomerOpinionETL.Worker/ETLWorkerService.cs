namespace CustomerOpinionETL.Worker;

using CustomerOpinionETL.Application.UseCases;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ETLWorkerService : BackgroundService
{
    private readonly ILogger<ETLWorkerService> _logger;
    private readonly ETLOrchestrator _etlOrchestrator;
    private readonly LoadDimensionsUseCase _loadDimensionsUseCase;
    private readonly IHostApplicationLifetime _appLifetime;

    public ETLWorkerService(
        ILogger<ETLWorkerService> logger,
        ETLOrchestrator etlOrchestrator,
        LoadDimensionsUseCase loadDimensionsUseCase,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _etlOrchestrator = etlOrchestrator;
        _loadDimensionsUseCase = loadDimensionsUseCase;
        _appLifetime = appLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("______________________________________________________");
            _logger.LogInformation("|   ETL WORKER SERVICE - CUSTOMER OPINION ANALYSIS   |");
            _logger.LogInformation("|____________________________________________________|");
            _logger.LogInformation("");
            _logger.LogInformation("Starting ETL process at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("");

            // Ejecutar el proceso ETL completo
            var summary = await _etlOrchestrator.ExecuteAsync(stoppingToken);

            // Mostrar resumen
            _logger.LogInformation("");
            _logger.LogInformation(summary.GetSummary());

            if (summary.Success)
            {
                _logger.LogInformation("");
                _logger.LogInformation("ETL Process completed successfully!");
                _logger.LogInformation("Process will now terminate.");
            }
            else
            {
                _logger.LogError("ETL Process completed with errors!");
                _logger.LogError("Check the logs for details.");
            }

            // Detener la aplicación después de completar
            _appLifetime.StopApplication();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ETL process was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ETL Worker Service");
            _appLifetime.StopApplication();
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ETL Worker Service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}
