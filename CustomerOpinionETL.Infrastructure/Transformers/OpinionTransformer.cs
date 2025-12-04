namespace CustomerOpinionETL.Infrastructure.Transformers;

using System.Globalization;
using CustomerOpinionETL.Application.Interfaces.Transformation;
using CustomerOpinionETL.Domain.Entities;
using CustomerOpinionETL.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

public class OpinionTransformer : IOpinionTransformer
{
    private readonly ILogger<OpinionTransformer> _logger;
    private readonly ISentimentAnalyzer _sentimentAnalyzer;

    public OpinionTransformer(
        ILogger<OpinionTransformer> logger,
        ISentimentAnalyzer sentimentAnalyzer)
    {
        _logger = logger;
        _sentimentAnalyzer = sentimentAnalyzer;
    }

    public async Task<Opinion> TransformAsync(OpinionRaw raw)
    {
        try
        {
            // 1. Limpiar y validar datos
            var comentarioLimpio = LimpiarComentario(raw.ComentarioRaw);

            // 2. Normalizar IDs
            var clienteId = NormalizarClienteId(raw.ClienteIdRaw);
            var productoId = NormalizarProductoId(raw.ProductoIdRaw);

            // 3. Parsear fecha
            var fecha = ParsearFecha(raw.FechaRaw);

            // 4. Determinar clasificación y puntaje
            string clasificacion;
            decimal puntajeSatisfaccion;

            // Si ya viene clasificado (CSV de surveys)
            if (!string.IsNullOrWhiteSpace(raw.ClasificacionRaw))
            {
                clasificacion = NormalizarClasificacion(raw.ClasificacionRaw);
                puntajeSatisfaccion = ParsearPuntaje(raw.RatingRaw, clasificacion);
            }
            // Si viene con Rating (web reviews)
            else if (!string.IsNullOrWhiteSpace(raw.RatingRaw))
            {
                var rating = ParsearRating(raw.RatingRaw);
                clasificacion = ConvertirRatingAClasificacion(rating);
                puntajeSatisfaccion = rating;
            }
            // Si no tiene clasificación ni rating, analizar sentimiento (API, social media)
            else
            {
                var sentimiento = await _sentimentAnalyzer.AnalyzeAsync(comentarioLimpio);
                clasificacion = sentimiento.Clasificacion;
                puntajeSatisfaccion = ConvertirSentimentScoreAPuntaje(sentimiento.Score);
            }

            // 5. Crear la opinión transformada
            var opinion = new Opinion
            {
                IdCliente = clienteId,
                IdProducto = productoId,
                Fecha = fecha,
                Comentario = comentarioLimpio,
                ClasificacionSentimiento = clasificacion,
                PuntajeSatisfaccion = puntajeSatisfaccion,
                CanalOriginal = DeterminarCanal(raw),
                IdFuente = 0 // Se asignará al cargar
            };

            _logger.LogDebug("Transformed opinion: Cliente={ClienteId}, Producto={ProductoId}, Clasificacion={Clasificacion}",
                clienteId, productoId, clasificacion);

            return opinion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming opinion from {Source}", raw.FuenteOrigen);
            throw;
        }
    }

    public async Task<IEnumerable<Opinion>> TransformBatchAsync(IEnumerable<OpinionRaw> raws)
    {
        var transformed = new List<Opinion>();

        foreach (var raw in raws)
        {
            try
            {
                var opinion = await TransformAsync(raw);
                transformed.Add(opinion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping opinion from {Source} due to transformation error", raw.FuenteOrigen);
            }
        }

        return transformed;
    }

    // =============================================
    // MÉTODOS DE LIMPIEZA Y NORMALIZACIÓN
    // =============================================

    private string LimpiarComentario(string? comentario)
    {
        if (string.IsNullOrWhiteSpace(comentario))
            return string.Empty;

        // Eliminar caracteres especiales problemáticos
        comentario = comentario.Trim();

        // Eliminar múltiples espacios
        comentario = System.Text.RegularExpressions.Regex.Replace(comentario, @"\s+", " ");

        // Limitar longitud (opcional)
        if (comentario.Length > 5000)
            comentario = comentario[..5000];

        return comentario;
    }

    private string NormalizarClienteId(string? rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId))
            return GenerarClienteAnonimo();

        rawId = rawId.Trim().ToUpper();

        // Si ya tiene prefijo C, dejarlo
        if (rawId.StartsWith("C"))
            return rawId;

        // Si es número, agregar prefijo C
        if (int.TryParse(rawId, out _))
            return $"C{rawId}";

        return rawId;
    }

    private string NormalizarProductoId(string? rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId))
            return "P0000"; // Producto desconocido

        rawId = rawId.Trim().ToUpper();

        // Si ya tiene prefijo P, dejarlo
        if (rawId.StartsWith("P"))
            return rawId;

        // Si es número, agregar prefijo P
        if (int.TryParse(rawId, out _))
            return $"P{rawId}";

        return rawId;
    }

    private string GenerarClienteAnonimo()
    {
        // Generar ID único para clientes anónimos (redes sociales)
        return $"CANON{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    // =============================================
    // MÉTODOS DE PARSING
    // =============================================

    private DateTime ParsearFecha(string? fechaRaw)
    {
        if (string.IsNullOrWhiteSpace(fechaRaw))
            return DateTime.Today;

        // Intentar formatos comunes
        string[] formatos = {
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "yyyy/MM/dd",
            "dd-MM-yyyy"
        };

        foreach (var formato in formatos)
        {
            if (DateTime.TryParseExact(fechaRaw, formato,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            {
                return fecha;
            }
        }

        // Intento general
        if (DateTime.TryParse(fechaRaw, out var fechaGeneral))
            return fechaGeneral;

        _logger.LogWarning("Could not parse date: {FechaRaw}, using today", fechaRaw);
        return DateTime.Today;
    }

    private decimal ParsearRating(string? ratingRaw)
    {
        if (string.IsNullOrWhiteSpace(ratingRaw))
            return 3m; // Neutral por defecto

        if (decimal.TryParse(ratingRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var rating))
        {
            // Asegurar que esté en rango 1-5
            return Math.Max(1m, Math.Min(5m, rating));
        }

        return 3m;
    }

    private decimal ParsearPuntaje(string? puntajeRaw, string clasificacion)
    {
        if (!string.IsNullOrWhiteSpace(puntajeRaw))
        {
            if (decimal.TryParse(puntajeRaw, NumberStyles.Any,
                CultureInfo.InvariantCulture, out var puntaje))
            {
                return Math.Max(1m, Math.Min(5m, puntaje));
            }
        }

        // Si no hay puntaje, inferir de la clasificación
        return clasificacion switch
        {
            "Positiva" => 4.5m,
            "Negativa" => 1.5m,
            _ => 3m
        };
    }

    // =============================================
    // CONVERSIONES DE CLASIFICACIÓN
    // =============================================

    private string NormalizarClasificacion(string clasificacionRaw)
    {
        return clasificacionRaw.ToLower().Trim() switch
        {
            "positiva" or "positive" => "Positiva",
            "negativa" or "negative" => "Negativa",
            "neutra" or "neutral" => "Neutral",
            _ => "Neutral"
        };
    }

    private string ConvertirRatingAClasificacion(decimal rating)
    {
        return rating switch
        {
            >= 4m => "Positiva",
            <= 2m => "Negativa",
            _ => "Neutral"
        };
    }

    private decimal ConvertirSentimentScoreAPuntaje(decimal sentimentScore)
    {
        // Convertir de escala -1 a 1 → escala 1 a 5
        // Formula: ((score + 1) / 2) * 4 + 1
        var puntaje = ((sentimentScore + 1m) / 2m) * 4m + 1m;
        return Math.Round(Math.Max(1m, Math.Min(5m, puntaje)), 2);
    }

    private string DeterminarCanal(OpinionRaw raw)
    {
        return raw.FuenteOrigen switch
        {
            "CSV" => "EncuestaInterna",
            "Database" => "Web",
            "API" => raw.MetadataAdicional.GetValueOrDefault("Platform", "RedSocial"),
            _ => "Desconocido"
        };
    }
}