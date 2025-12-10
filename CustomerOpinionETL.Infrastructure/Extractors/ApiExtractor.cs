namespace CustomerOpinionETL.Infrastructure.Extractors;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CustomerOpinionETL.Application.Interfaces.Extraction;
using CustomerOpinionETL.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ApiExtractor : IApiExtractor
{
    private readonly ILogger<ApiExtractor> _logger;
    private readonly ApiExtractorConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiExtractor(
        ILogger<ApiExtractor> logger,
        IOptions<ApiExtractorConfiguration> config,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
    }

    public string SourceName => "API REST (Social Media Comments)";

    public async Task<IEnumerable<OpinionRaw>> ExtractAsync(CancellationToken cancellationToken = default)
    {
        var opinions = new List<OpinionRaw>();

        try
        {
            _logger.LogInformation("Connecting to API: {BaseUrl}", _config.BaseUrl);

            var httpClient = _httpClientFactory.CreateClient("SocialMediaAPI");

            // Configurar headers
            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            }

            // Construir URL con query parameters
            var requestUrl = $"{_config.BaseUrl}{_config.Endpoint}";
            if (!string.IsNullOrEmpty(_config.QueryParameters))
            {
                requestUrl += $"?{_config.QueryParameters}";
            }

            _logger.LogInformation("Requesting: {Url}", requestUrl);

            // Hacer request a la API
            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Parsear respuesta
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken);

            if (apiResponse?.Comments != null)
            {
                foreach (var comment in apiResponse.Comments)
                {
                    opinions.Add(MapApiCommentToOpinionRaw(comment));
                }
            }

            _logger.LogInformation("✓ Extracted {Count} records from API", opinions.Count);
            return opinions;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error extracting from API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting from API");
            throw;
        }
    }

    private OpinionRaw MapApiCommentToOpinionRaw(SocialMediaComment comment)
    {
        return new OpinionRaw
        {
            IdOriginal = comment.Id ?? "",
            ClienteIdRaw = comment.UserId,
            ProductoIdRaw = comment.ProductId,
            FechaRaw = comment.CreatedAt,
            ComentarioRaw = comment.Text,
            FuenteOrigen = "API",
            MetadataAdicional = new Dictionary<string, string>
            {
                ["Platform"] = comment.Platform ?? "Unknown",
                ["Likes"] = comment.Likes?.ToString() ?? "0",
                ["Shares"] = comment.Shares?.ToString() ?? "0",
                ["UserHandle"] = comment.UserHandle ?? ""
            }
        };
    }

    // DTOs para la API
    private class ApiResponse
    {
        [JsonPropertyName("data")]
        public List<SocialMediaComment>? Comments { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    private class SocialMediaComment
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("user_handle")]
        public string? UserHandle { get; set; }

        [JsonPropertyName("product_id")]
        public string? ProductId { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("likes")]
        public int? Likes { get; set; }

        [JsonPropertyName("shares")]
        public int? Shares { get; set; }
    }
}

public class ApiExtractorConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "/api/comments";
    public string? ApiKey { get; set; }
    public string? QueryParameters { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}