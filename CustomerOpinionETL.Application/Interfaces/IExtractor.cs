namespace CustomerOpinionETL.Application.Interfaces.Extraction;

using CustomerOpinionETL.Domain.ValueObjects;

public interface IExtractor
{
    string SourceName { get; }
    Task<IEnumerable<OpinionRaw>> ExtractAsync(CancellationToken cancellationToken = default);
}

// Extractores específicos
public interface ICsvExtractor : IExtractor { }
public interface IDatabaseExtractor : IExtractor { }
public interface IApiExtractor : IExtractor { }