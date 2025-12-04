namespace CustomerOpinionETL.Domain.ValueObjects;

public class SentimentScore
{
    public string Clasificacion { get; private set; }
    public decimal Score { get; private set; }
    public decimal Positivo { get; private set; }
    public decimal Negativo { get; private set; }
    public decimal Neutral { get; private set; }

    public SentimentScore(decimal score, decimal positivo, decimal negativo, decimal neutral)
    {
        Score = score;
        Positivo = positivo;
        Negativo = negativo;
        Neutral = neutral;
        Clasificacion = DeterminarClasificacion(score);
    }

    private string DeterminarClasificacion(decimal score)
    {
        return score switch
        {
            >= 0.2m => "Positiva",
            <= -0.2m => "Negativa",
            _ => "Neutral"
        };
    }
}