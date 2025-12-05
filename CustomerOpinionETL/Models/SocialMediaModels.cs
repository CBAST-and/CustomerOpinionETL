namespace CustomerOpinionETL.API.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Respuesta de la API con lista de comentarios
/// </summary>
public class SocialMediaResponse
{
    [JsonPropertyName("data")]
    public List<SocialMediaComment> Data { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }
}

/// <summary>
/// Comentario de redes sociales
/// </summary>
public class SocialMediaComment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("user_handle")]
    public string? UserHandle { get; set; }

    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("likes")]
    public int Likes { get; set; }

    [JsonPropertyName("shares")]
    public int Shares { get; set; }

    [JsonPropertyName("sentiment")]
    public string? Sentiment { get; set; }
}

/// <summary>
/// Parámetros de consulta para filtrar comentarios
/// </summary>
public class SocialMediaQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Platform { get; set; } // Instagram, Twitter, Facebook, all
    public string? ProductId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Limit { get; set; } = 1000;
}

/// <summary>
/// Modelo interno para leer del CSV
/// </summary>
public class SocialCommentCsvRow
{
    public string IdComment { get; set; } = string.Empty;
    public string? IdCliente { get; set; }
    public string IdProducto { get; set; } = string.Empty;
    public string Fuente { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
}