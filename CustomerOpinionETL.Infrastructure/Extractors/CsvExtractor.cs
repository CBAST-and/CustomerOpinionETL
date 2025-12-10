namespace CustomerOpinionETL.Infrastructure.Extractors;

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CustomerOpinionETL.Application.Interfaces.Extraction;
using CustomerOpinionETL.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class CsvExtractor : ICsvExtractor
{
    private readonly ILogger<CsvExtractor> _logger;
    private readonly CsvExtractorConfiguration _config;

    public CsvExtractor(
        ILogger<CsvExtractor> logger,
        IOptions<CsvExtractorConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public string SourceName => "CSV Files (Encuestas Internas)";

    public async Task<IEnumerable<OpinionRaw>> ExtractAsync(CancellationToken cancellationToken = default)
    {
        var allOpinions = new List<OpinionRaw>();

        try
        {
            // SOLO procesar archivos de encuestas internas (surveys)
            var csvFiles = Directory.GetFiles(_config.CsvFolderPath, "surveys*.csv");

            if (csvFiles.Length == 0)
            {
                _logger.LogWarning("No survey CSV files found in {Path}", _config.CsvFolderPath);
                return allOpinions;
            }

            _logger.LogInformation("Found {Count} survey CSV files in {Path}", csvFiles.Length, _config.CsvFolderPath);

            foreach (var csvFile in csvFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var fileName = Path.GetFileName(csvFile);
                _logger.LogInformation("Processing survey file: {FileName}", fileName);

                var opinions = await ExtractFromCsvFileAsync(csvFile, cancellationToken);
                allOpinions.AddRange(opinions);
            }

            _logger.LogInformation("Extracted {Count} opinions from survey CSV files", allOpinions.Count);
            return allOpinions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting from CSV files");
            throw;
        }
    }

    private async Task<List<OpinionRaw>> ExtractFromCsvFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var opinions = new List<OpinionRaw>();
        var fileName = Path.GetFileName(filePath);

        try
        {
            _logger.LogInformation("Reading CSV file: {FileName}", fileName);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? Array.Empty<string>();

            while (await csv.ReadAsync())
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var opinion = MapCsvRecordToOpinionRaw(csv, headers, fileName);
                    opinions.Add(opinion);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading CSV row in file {FileName}", fileName);
                }
            }

            _logger.LogInformation("✓ Extracted {Count} records from {FileName}", opinions.Count, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV file: {FileName}", fileName);
        }

        return opinions;
    }

    private OpinionRaw MapCsvRecordToOpinionRaw(CsvReader csv, string[] headers, string fileName)
    {
        var opinion = new OpinionRaw
        {
            FuenteOrigen = "CSV",
            MetadataAdicional = { ["FileName"] = fileName }
        };

        // Mapeo flexible basado en los headers disponibles
        foreach (var header in headers)
        {
            var value = csv.GetField(header);

            switch (header.ToLower().Trim())
            {
                case "idopinion":
                    opinion.IdOriginal = value ?? "";
                    break;
                case "idcliente":
                    opinion.ClienteIdRaw = value;
                    break;
                case "idproducto":
                    opinion.ProductoIdRaw = value;
                    break;
                case "fecha":
                    opinion.FechaRaw = value;
                    break;
                case "comentario":
                    opinion.ComentarioRaw = value;
                    break;
                case "clasificación":
                case "clasificacion":
                    opinion.ClasificacionRaw = value;
                    break;
                case "puntajesatisfacción":
                case "puntajesatisfaccion":
                case "rating":
                    opinion.RatingRaw = value;
                    break;
            }
        }

        return opinion;
    }
}

public class CsvExtractorConfiguration
{
    public string CsvFolderPath { get; set; } = "./data";
}