namespace CustomerOpinionETL.API.Services;

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CustomerOpinionETL.API.Models;

public interface ISocialMediaDataService
{
    Task<SocialMediaResponse> GetCommentsAsync(SocialMediaQueryParams queryParams);
    Task<SocialMediaComment?> GetCommentByIdAsync(string id);
    Task<int> GetTotalCountAsync();
}

public class SocialMediaDataService : ISocialMediaDataService
{
    private readonly ILogger<SocialMediaDataService> _logger;
    private readonly string _csvFilePath;
    private List<SocialMediaComment>? _cachedComments;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public SocialMediaDataService(
        ILogger<SocialMediaDataService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _csvFilePath = configuration["DataSource:CsvFilePath"]
            ?? throw new InvalidOperationException("CSV file path not configured");
    }

    public async Task<SocialMediaResponse> GetCommentsAsync(SocialMediaQueryParams queryParams)
    {
        var comments = await GetAllCommentsAsync();

        // Aplicar filtros usando List<T> en lugar de IQueryable
        var filtered = comments.AsEnumerable();

        // Filtro por plataforma
        if (!string.IsNullOrEmpty(queryParams.Platform) && queryParams.Platform.ToLower() != "all")
        {
            filtered = filtered.Where(c =>
                c.Platform.Equals(queryParams.Platform, StringComparison.OrdinalIgnoreCase));
        }

        // Filtro por producto
        if (!string.IsNullOrEmpty(queryParams.ProductId))
        {
            filtered = filtered.Where(c => c.ProductId == queryParams.ProductId);
        }

        // Filtro por fecha de inicio
        if (queryParams.StartDate.HasValue)
        {
            filtered = filtered.Where(c =>
            {
                if (DateTime.TryParse(c.CreatedAt, out DateTime date))
                {
                    return date >= queryParams.StartDate.Value;
                }
                return false;
            });
        }

        // Filtro por fecha de fin
        if (queryParams.EndDate.HasValue)
        {
            filtered = filtered.Where(c =>
            {
                if (DateTime.TryParse(c.CreatedAt, out DateTime date))
                {
                    return date <= queryParams.EndDate.Value;
                }
                return false;
            });
        }

        // Convertir a lista para contar
        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        // Aplicar paginación
        var pageSize = Math.Min(queryParams.PageSize, 100); // Máximo 100 por página
        var page = Math.Max(1, queryParams.Page);
        var skip = (page - 1) * pageSize;

        var pagedComments = filteredList
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        _logger.LogInformation("Returned {Count} comments (page {Page} of {TotalPages})",
            pagedComments.Count, page, (int)Math.Ceiling(totalFiltered / (double)pageSize));

        return new SocialMediaResponse
        {
            Data = pagedComments,
            Total = totalFiltered,
            Page = page,
            PageSize = pageSize,
            HasNext = skip + pageSize < totalFiltered
        };
    }

    public async Task<SocialMediaComment?> GetCommentByIdAsync(string id)
    {
        var comments = await GetAllCommentsAsync();
        return comments.FirstOrDefault(c => c.Id == id);
    }

    public async Task<int> GetTotalCountAsync()
    {
        var comments = await GetAllCommentsAsync();
        return comments.Count;
    }

    private async Task<List<SocialMediaComment>> GetAllCommentsAsync()
    {
        // Implementar caché simple
        if (_cachedComments != null)
            return _cachedComments;

        await _cacheLock.WaitAsync();
        try
        {
            if (_cachedComments != null)
                return _cachedComments;

            _logger.LogInformation("Loading comments from CSV: {FilePath}", _csvFilePath);

            if (!File.Exists(_csvFilePath))
            {
                _logger.LogWarning("CSV file not found: {FilePath}", _csvFilePath);
                return new List<SocialMediaComment>();
            }

            var comments = new List<SocialMediaComment>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using (var reader = new StreamReader(_csvFilePath))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<SocialCommentCsvRow>();

                foreach (var record in records)
                {
                    var comment = MapCsvRowToComment(record);
                    comments.Add(comment);
                }
            }

            _cachedComments = comments;
            _logger.LogInformation("Loaded and cached {Count} comments", comments.Count);

            return comments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading CSV file");
            return new List<SocialMediaComment>();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private SocialMediaComment MapCsvRowToComment(SocialCommentCsvRow row)
    {
        // Generar datos realistas para likes y shares
        var random = new Random(row.IdComment.GetHashCode());
        var likes = random.Next(0, 500);
        var shares = random.Next(0, 50);

        // Normalizar IDs
        var userId = string.IsNullOrWhiteSpace(row.IdCliente)
            ? null
            : NormalizeId(row.IdCliente, "C");

        var productId = NormalizeId(row.IdProducto, "P");

        // Generar user handle basado en el userId
        var userHandle = userId != null
            ? $"@user_{userId.Replace("C", "")}"
            : $"@anonymous_{random.Next(1000, 9999)}";

        return new SocialMediaComment
        {
            Id = row.IdComment,
            UserId = userId,
            UserHandle = userHandle,
            ProductId = productId,
            Text = row.Comentario,
            Platform = row.Fuente,
            CreatedAt = row.Fecha,
            Likes = likes,
            Shares = shares,
            Sentiment = null // El ETL lo analizará
        };
    }

    private string NormalizeId(string rawId, string prefix)
    {
        if (string.IsNullOrWhiteSpace(rawId))
            return $"{prefix}0000";

        rawId = rawId.Trim().ToUpper();

        if (rawId.StartsWith(prefix))
            return rawId;

        if (int.TryParse(rawId, out _))
            return $"{prefix}{rawId}";

        return rawId;
    }

    public void ClearCache()
    {
        _cacheLock.Wait();
        try
        {
            _cachedComments = null;
            _logger.LogInformation("Cache cleared");
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}