namespace CustomerOpinionETL.Application.DTOs;

public class ExtractionResult
{
    public string SourceName { get; set; } = string.Empty;
    public int RecordsExtracted { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}