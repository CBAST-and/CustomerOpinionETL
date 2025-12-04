namespace CustomerOpinionETL.Infrastructure.Extractors;

using System.Data;
using Dapper;
using CustomerOpinionETL.Application.Interfaces.Extraction;
using CustomerOpinionETL.Domain.ValueObjects;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class DatabaseExtractor : IDatabaseExtractor
{
    private readonly ILogger<DatabaseExtractor> _logger;
    private readonly DatabaseExtractorConfiguration _config;

    public DatabaseExtractor(
        ILogger<DatabaseExtractor> logger,
        IOptions<DatabaseExtractorConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public string SourceName => "Database (Web Reviews)";

    public async Task<IEnumerable<OpinionRaw>> ExtractAsync(CancellationToken cancellationToken = default)
    {
        var opinions = new List<OpinionRaw>();

        try
        {
            _logger.LogInformation("Connecting to source database...");

            using var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Query para extraer reseñas web
            var sql = _config.Query ?? @"
                SELECT 
                    IdReview,
                    IdCliente,
                    IdProducto,
                    Fecha,
                    Comentario,
                    Rating,
                    IsVerified
                FROM WebReviews
                WHERE Fecha >= @StartDate";

            var parameters = new
            {
                StartDate = _config.StartDate ?? DateTime.Now.AddYears(-1)
            };

            _logger.LogInformation("Executing query: {Query}", sql);

            var records = await connection.QueryAsync<WebReviewRecord>(
                sql,
                parameters,
                commandTimeout: _config.TimeoutSeconds);

            foreach (var record in records)
            {
                opinions.Add(MapDatabaseRecordToOpinionRaw(record));
            }

            _logger.LogInformation("✓ Extracted {Count} records from database", opinions.Count);
            return opinions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting from database");
            throw;
        }
    }

    private OpinionRaw MapDatabaseRecordToOpinionRaw(WebReviewRecord record)
    {
        return new OpinionRaw
        {
            IdOriginal = record.IdReview ?? "",
            ClienteIdRaw = record.IdCliente,
            ProductoIdRaw = record.IdProducto,
            FechaRaw = record.Fecha?.ToString("yyyy-MM-dd"),
            ComentarioRaw = record.Comentario,
            RatingRaw = record.Rating?.ToString(),
            FuenteOrigen = "Database",
            MetadataAdicional = new Dictionary<string, string>
            {
                ["IsVerified"] = record.IsVerified?.ToString() ?? "false",
                ["Source"] = "WebReviews"
            }
        };
    }

    private class WebReviewRecord
    {
        public string? IdReview { get; set; }
        public string? IdCliente { get; set; }
        public string? IdProducto { get; set; }
        public DateTime? Fecha { get; set; }
        public string? Comentario { get; set; }
        public int? Rating { get; set; }
        public bool? IsVerified { get; set; }
    }
}

public class DatabaseExtractorConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string? Query { get; set; }
    public DateTime? StartDate { get; set; }
    public int TimeoutSeconds { get; set; } = 60;
}