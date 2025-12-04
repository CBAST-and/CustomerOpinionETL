namespace CustomerOpinionETL.Infrastructure.Services;

public class SentimentStatistics
{
    public int TotalAnalyzed { get; set; }
    public int Positivos { get; set; }
    public int Negativos { get; set; }
    public int Neutrales { get; set; }
    public double PromedioScore { get; set; }

    public double PorcentajePositivo => TotalAnalyzed > 0 ? (Positivos * 100.0 / TotalAnalyzed) : 0;
    public double PorcentajeNegativo => TotalAnalyzed > 0 ? (Negativos * 100.0 / TotalAnalyzed) : 0;
    public double PorcentajeNeutral => TotalAnalyzed > 0 ? (Neutrales * 100.0 / TotalAnalyzed) : 0;

    public override string ToString()
    {
        return $@"Sentiment Statistics:
  Total Analyzed: {TotalAnalyzed}
  Positivos: {Positivos} ({PorcentajePositivo:F2}%)
  Negativos: {Negativos} ({PorcentajeNegativo:F2}%)
  Neutrales: {Neutrales} ({PorcentajeNeutral:F2}%)
  Promedio Score: {PromedioScore:F3}";
    }
}