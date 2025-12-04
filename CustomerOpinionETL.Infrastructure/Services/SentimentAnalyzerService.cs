namespace CustomerOpinionETL.Infrastructure.Services;

using CustomerOpinionETL.Application.Interfaces.Transformation;
using CustomerOpinionETL.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using VaderSharp2;

public class SentimentAnalyzerService : ISentimentAnalyzer
{
    private readonly ILogger<SentimentAnalyzerService> _logger;
    private readonly SentimentIntensityAnalyzer _vaderAnalyzer;

    // Diccionarios de palabras clave en español
    private readonly Dictionary<string, double> _palabrasPositivas = new()
    {
        // Muy positivas
        { "excelente", 0.9 }, { "increíble", 0.9 }, { "perfecto", 0.9 },
        { "maravilloso", 0.85 }, { "fantástico", 0.85 }, { "excepcional", 0.9 },
        { "espectacular", 0.85 }, { "impresionante", 0.8 },
        
        // Positivas
        { "genial", 0.7 }, { "bueno", 0.6 }, { "bien", 0.5 },
        { "recomendable", 0.7 }, { "recomiendo", 0.7 }, { "satisfecho", 0.7 },
        { "contento", 0.7 }, { "feliz", 0.7 }, { "encantado", 0.8 },
        
        // Calidad y características
        { "calidad", 0.5 }, { "rápido", 0.5 }, { "funciona", 0.4 },
        { "mejor", 0.6 }, { "gran", 0.5 }, { "útil", 0.5 },
        { "práctico", 0.5 }, { "eficiente", 0.6 }, { "duradero", 0.6 },
        
        // Emociones positivas
        { "me gusta", 0.6 }, { "me encanta", 0.8 }, { "amo", 0.8 },
        { "perfección", 0.9 }, { "superó expectativas", 0.8 }
    };

    private readonly Dictionary<string, double> _palabrasNegativas = new()
    {
        // Muy negativas
        { "pésimo", -0.9 }, { "terrible", -0.9 }, { "horrible", -0.9 },
        { "basura", -0.95 }, { "fraude", -0.95 }, { "estafa", -0.95 },
        { "desastre", -0.85 }, { "nefasto", -0.85 },
        
        // Negativas
        { "malo", -0.7 }, { "mal", -0.6 }, { "defectuoso", -0.8 },
        { "roto", -0.7 }, { "decepcionante", -0.7 }, { "decepcionado", -0.7 },
        { "insatisfecho", -0.7 }, { "molesto", -0.6 },
        
        // Problemas
        { "no funciona", -0.8 }, { "no sirve", -0.8 }, { "falló", -0.7 },
        { "falla", -0.7 }, { "error", -0.6 }, { "problema", -0.6 },
        { "defecto", -0.7 }, { "rompió", -0.7 }, { "dañado", -0.7 },
        
        // Recomendación negativa
        { "no recomiendo", -0.8 }, { "no lo compren", -0.85 },
        { "mala calidad", -0.8 }, { "peor", -0.6 },
        
        // Tiempo
        { "lento", -0.5 }, { "demora", -0.5 }, { "tardó", -0.4 }
    };

    public SentimentAnalyzerService(ILogger<SentimentAnalyzerService> logger)
    {
        _logger = logger;
        _vaderAnalyzer = new SentimentIntensityAnalyzer();
    }

    public async Task<SentimentScore> AnalyzeAsync(string texto)
    {
        return await Task.Run(() => AnalizarSentimiento(texto));
    }

    public async Task<IEnumerable<SentimentScore>> AnalyzeBatchAsync(IEnumerable<string> textos)
    {
        var tasks = textos.Select(texto => AnalyzeAsync(texto));
        return await Task.WhenAll(tasks);
    }

    private SentimentScore AnalizarSentimiento(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return new SentimentScore(0, 0, 0, 1);
        }

        try
        {
            // 1. Análisis con VADER (funciona bien con inglés y algo de español)
            var vaderResults = _vaderAnalyzer.PolarityScores(texto);

            // 2. Análisis con palabras clave en español
            var spanishScore = AnalizarPalabrasClaveEspañol(texto.ToLower());

            // 3. Combinar ambos análisis
            // Dar más peso al análisis en español (70%) vs VADER (30%)
            var scoreCompuesto = (spanishScore * 0.7) + (vaderResults.Compound * 0.3);

            // 4. Normalizar valores
            var positivo = (decimal)Math.Max(0, scoreCompuesto);
            var negativo = (decimal)Math.Max(0, -scoreCompuesto);
            var neutral = (decimal)(1 - Math.Abs(scoreCompuesto));

            _logger.LogDebug("Sentiment analysis: Text='{Text}', Score={Score}",
                texto.Length > 50 ? texto[..50] + "..." : texto,
                scoreCompuesto);

            return new SentimentScore(
                (decimal)scoreCompuesto,
                positivo,
                negativo,
                neutral
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment for text: {Text}", texto);
            return new SentimentScore(0, 0, 0, 1);
        }
    }

    private double AnalizarPalabrasClaveEspañol(string textoLower)
    {
        double scoreTotal = 0;
        int palabrasEncontradas = 0;
        int modificadorNegacion = 1;

        var palabras = textoLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < palabras.Length; i++)
        {
            var palabra = palabras[i].Trim(',', '.', '!', '?', ';', ':');

            // Detectar negaciones (invierte el sentimiento)
            if (EsNegacion(palabra))
            {
                modificadorNegacion = -1;
                continue;
            }

            // Buscar en palabras positivas
            if (_palabrasPositivas.TryGetValue(palabra, out var valorPositivo))
            {
                scoreTotal += valorPositivo * modificadorNegacion;
                palabrasEncontradas++;
                modificadorNegacion = 1; // Reset después de aplicar
            }
            // Buscar en palabras negativas
            else if (_palabrasNegativas.TryGetValue(palabra, out var valorNegativo))
            {
                scoreTotal += valorNegativo * modificadorNegacion;
                palabrasEncontradas++;
                modificadorNegacion = 1; // Reset después de aplicar
            }
            // Buscar frases compuestas
            else if (i < palabras.Length - 1)
            {
                var frase = $"{palabra} {palabras[i + 1]}";

                if (_palabrasPositivas.TryGetValue(frase, out var valorFrasePos))
                {
                    scoreTotal += valorFrasePos * modificadorNegacion;
                    palabrasEncontradas++;
                    modificadorNegacion = 1;
                    i++; // Saltar siguiente palabra
                }
                else if (_palabrasNegativas.TryGetValue(frase, out var valorFraseNeg))
                {
                    scoreTotal += valorFraseNeg * modificadorNegacion;
                    palabrasEncontradas++;
                    modificadorNegacion = 1;
                    i++; // Saltar siguiente palabra
                }
            }
        }

        // Si no encontramos palabras clave, retornar neutral
        if (palabrasEncontradas == 0)
            return 0;

        // Normalizar el score al rango -1 a 1
        var scorePromedio = scoreTotal / palabrasEncontradas;
        return Math.Max(-1, Math.Min(1, scorePromedio));
    }

    private bool EsNegacion(string palabra)
    {
        return palabra switch
        {
            "no" or "nunca" or "jamás" or "tampoco" or "ni" or "sin" => true,
            _ => false
        };
    }
}