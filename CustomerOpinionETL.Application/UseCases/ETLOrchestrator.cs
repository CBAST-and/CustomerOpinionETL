namespace CustomerOpinionETL.Application.UseCases;

using CustomerOpinionETL.Application.DTOs;
using CustomerOpinionETL.Application.Interfaces;
using CustomerOpinionETL.Application.Interfaces.Extraction;
using CustomerOpinionETL.Application.Interfaces.Loading;
using CustomerOpinionETL.Application.Interfaces.Transformation;
using CustomerOpinionETL.Domain.Entities;
using CustomerOpinionETL.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

public class ETLOrchestrator
{
    private readonly ILogger<ETLOrchestrator> _logger;
    private readonly ICsvExtractor _csvExtractor;
    private readonly IDatabaseExtractor _databaseExtractor;
    private readonly IApiExtractor _apiExtractor;
    private readonly IOpinionTransformer _transformer;
    private readonly IUnitOfWork _unitOfWork;

    public ETLOrchestrator(
        ILogger<ETLOrchestrator> logger,
        ICsvExtractor csvExtractor,
        IDatabaseExtractor databaseExtractor,
        IApiExtractor apiExtractor,
        IOpinionTransformer transformer,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _csvExtractor = csvExtractor;
        _databaseExtractor = databaseExtractor;
        _apiExtractor = apiExtractor;
        _transformer = transformer;
        _unitOfWork = unitOfWork;
    }

    public async Task<ETLExecutionSummary> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var summary = new ETLExecutionSummary
        {
            ExecutionStartTime = DateTime.Now
        };

        try
        {
            _logger.LogInformation("=== Starting ETL Process ===");

            // FASE 1: EXTRACCIÓN (3 fuentes)
            var extractedData = await ExtractFromAllSourcesAsync(summary, cancellationToken);

            // FASE 2: TRANSFORMACIÓN
            var transformedData = await TransformDataAsync(extractedData, summary, cancellationToken);

            // FASE 3: CARGA
            await LoadDataAsync(transformedData, summary, cancellationToken);

            summary.Success = true;
            summary.TotalRecordsProcessed = transformedData.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL Process failed");
            summary.Success = false;
        }
        finally
        {
            summary.ExecutionEndTime = DateTime.Now;
            _logger.LogInformation(summary.GetSummary());
        }

        return summary;
    }

    // =============================================
    // FASE 1: EXTRACCIÓN
    // =============================================

    private async Task<List<OpinionRaw>> ExtractFromAllSourcesAsync(
        ETLExecutionSummary summary,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- PHASE 1: EXTRACTION ---");
        var allData = new List<OpinionRaw>();

        // 1. Extraer de CSV
        summary.CsvExtraction = await ExtractFromSourceAsync(
            _csvExtractor,
            allData,
            cancellationToken);

        // 2. Extraer de Base de Datos
        summary.DatabaseExtraction = await ExtractFromSourceAsync(
            _databaseExtractor,
            allData,
            cancellationToken);

        // 3. Extraer de API REST
        summary.ApiExtraction = await ExtractFromSourceAsync(
            _apiExtractor,
            allData,
            cancellationToken);

        _logger.LogInformation("Total extracted: {Count} records", allData.Count);
        return allData;
    }

    private async Task<ExtractionResult> ExtractFromSourceAsync(
        IExtractor extractor,
        List<OpinionRaw> allData,
        CancellationToken cancellationToken)
    {
        var result = new ExtractionResult
        {
            SourceName = extractor.SourceName,
            StartTime = DateTime.Now
        };

        try
        {
            _logger.LogInformation("Extracting from: {Source}", extractor.SourceName);

            var data = await extractor.ExtractAsync(cancellationToken);
            allData.AddRange(data);

            result.RecordsExtracted = data.Count();
            result.Success = true;

            _logger.LogInformation("✓ Extracted {Count} records from {Source}",
                result.RecordsExtracted, extractor.SourceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting from {Source}", extractor.SourceName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    // =============================================
    // FASE 2: TRANSFORMACIÓN
    // =============================================

    private async Task<List<Opinion>> TransformDataAsync(
        List<OpinionRaw> rawData,
        ETLExecutionSummary summary,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- PHASE 2: TRANSFORMATION ---");

        var result = new TransformationResult
        {
            StartTime = DateTime.Now
        };

        var transformedData = new List<Opinion>();

        try
        {
            foreach (var raw in rawData)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var transformed = await _transformer.TransformAsync(raw);
                    transformedData.Add(transformed);
                    result.RecordsTransformed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to transform record from {Source}", raw.FuenteOrigen);
                    result.RecordsSkipped++;
                    result.Errors.Add($"Source: {raw.FuenteOrigen}, Error: {ex.Message}");
                }
            }

            result.Success = true;
            _logger.LogInformation("✓ Transformed {Count} records, Skipped {Skipped}",
                result.RecordsTransformed, result.RecordsSkipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transformation phase failed");
            result.Success = false;
        }
        finally
        {
            result.EndTime = DateTime.Now;
            summary.Transformation = result;
        }

        return transformedData;
    }

    // =============================================
    // FASE 3: CARGA
    // =============================================

    private async Task LoadDataAsync(
        List<Opinion> opinions,
        ETLExecutionSummary summary,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- PHASE 3: LOADING ---");

        var result = new LoadingResult
        {
            StartTime = DateTime.Now
        };

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Paso 1: Cargar dimensiones únicas de clientes y productos
            var clientesUnicos = opinions
                .GroupBy(o => o.IdCliente)
                .Select(g => new Cliente
                {
                    IdCliente = g.Key,
                    Nombre = $"Cliente_{g.Key}",
                    Email = null
                })
                .ToList();

            var productosUnicos = opinions
                .GroupBy(o => o.IdProducto)
                .Select(g => new Producto
                {
                    IdProducto = g.Key,
                    NombreProducto = $"Producto_{g.Key}",
                    Categoria = null,
                    Precio = null
                })
                .ToList();

            _logger.LogInformation("Loading {ClientCount} unique clients and {ProductCount} unique products",
                clientesUnicos.Count, productosUnicos.Count);

            foreach (var cliente in clientesUnicos)
                await _unitOfWork.Clientes.GetOrCreateAsync(cliente);

            foreach (var producto in productosUnicos)
                await _unitOfWork.Productos.GetOrCreateAsync(producto);

            // Paso 2: Cargar opiniones
            foreach (var opinion in opinions)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Asegurar dimensiones existan
                    opinion.IdFecha = await _unitOfWork.Fechas.GetFechaIdAsync(opinion.Fecha);
                    opinion.IdFuente = await _unitOfWork.Fuentes.GetOrCreateAsync(opinion.CanalOriginal ?? "Desconocido");

                    // Insertar opinión
                    await _unitOfWork.Opiniones.InsertAsync(opinion);
                    result.RecordsLoaded++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load opinion for client {ClientId}", opinion.IdCliente);
                    result.RecordsFailed++;
                    result.Errors.Add($"Cliente: {opinion.IdCliente}, Error: {ex.Message}");
                }
            }

            await _unitOfWork.CommitTransactionAsync();

            result.Success = true;
            _logger.LogInformation("✓ Loaded {Count} records, Failed {Failed}",
                result.RecordsLoaded, result.RecordsFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loading phase failed");
            await _unitOfWork.RollbackTransactionAsync();
            result.Success = false;
        }
        finally
        {
            result.EndTime = DateTime.Now;
            summary.Loading = result;
        }
    }
}