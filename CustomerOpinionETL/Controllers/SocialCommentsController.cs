namespace CustomerOpinionETL.API.Controllers;

using CustomerOpinionETL.API.Models;
using CustomerOpinionETL.API.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/social-comments")]
[Produces("application/json")]
public class SocialCommentsController : ControllerBase
{
    private readonly ILogger<SocialCommentsController> _logger;
    private readonly ISocialMediaDataService _dataService;

    public SocialCommentsController(
        ILogger<SocialCommentsController> logger,
        ISocialMediaDataService dataService)
    {
        _logger = logger;
        _dataService = dataService;
    }

    /// <summary>
    /// Obtiene comentarios de redes sociales con filtros opcionales
    /// </summary>
    /// <param name="page">Número de página (default: 1)</param>
    /// <param name="pageSize">Tamaño de página (default: 50, max: 100)</param>
    /// <param name="platform">Plataforma: Instagram, Twitter, Facebook, all (default: all)</param>
    /// <param name="productId">Filtrar por ID de producto</param>
    /// <param name="startDate">Fecha inicial (formato: yyyy-MM-dd)</param>
    /// <param name="endDate">Fecha final (formato: yyyy-MM-dd)</param>
    /// <param name="limit">Límite total de resultados (default: 1000)</param>
    /// <returns>Lista paginada de comentarios</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SocialMediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SocialMediaResponse>> GetComments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? platform = "all",
        [FromQuery] string? productId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 1000)
    {
        try
        {
            _logger.LogInformation(
                "GET /api/social-comments - Page: {Page}, PageSize: {PageSize}, Platform: {Platform}",
                page, pageSize, platform);

            var queryParams = new SocialMediaQueryParams
            {
                Page = page,
                PageSize = pageSize,
                Platform = platform,
                ProductId = productId,
                StartDate = startDate,
                EndDate = endDate,
                Limit = limit
            };

            var response = await _dataService.GetCommentsAsync(queryParams);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social media comments");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtiene un comentario específico por ID
    /// </summary>
    /// <param name="id">ID del comentario (ej: T0001)</param>
    /// <returns>Comentario encontrado</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SocialMediaComment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SocialMediaComment>> GetCommentById(string id)
    {
        try
        {
            _logger.LogInformation("GET /api/social-comments/{Id}", id);

            var comment = await _dataService.GetCommentByIdAsync(id);

            if (comment == null)
            {
                _logger.LogWarning("Comment not found: {Id}", id);
                return NotFound(new { error = $"Comment with id '{id}' not found" });
            }

            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment by id: {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtiene el conteo total de comentarios
    /// </summary>
    /// <returns>Número total de comentarios disponibles</returns>
    [HttpGet("count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTotalCount()
    {
        try
        {
            _logger.LogInformation("GET /api/social-comments/count");

            var count = await _dataService.GetTotalCountAsync();

            return Ok(new { total = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total count");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Endpoint de health check
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "Social Media Comments API",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}

/// <summary>
/// Controller para información de la API
/// </summary>
[ApiController]
[Route("api")]
public class ApiInfoController : ControllerBase
{
    /// <summary>
    /// Información general de la API
    /// </summary>
    [HttpGet]
    public IActionResult GetApiInfo()
    {
        return Ok(new
        {
            name = "Customer Opinion Social Media API",
            version = "1.0.0",
            description = "Mock API que simula comentarios de redes sociales para el sistema ETL",
            endpoints = new
            {
                comments = "/api/social-comments",
                comment_by_id = "/api/social-comments/{id}",
                count = "/api/social-comments/count",
                health = "/api/social-comments/health"
            },
            documentation = "/swagger"
        });
    }
}