namespace CustomerOpinionETL.Application.Interfaces.Transformation;

using CustomerOpinionETL.Domain.ValueObjects;

public interface ISentimentAnalyzer
{
    Task<SentimentScore> AnalyzeAsync(string texto);
    Task<IEnumerable<SentimentScore>> AnalyzeBatchAsync(IEnumerable<string> textos);
}