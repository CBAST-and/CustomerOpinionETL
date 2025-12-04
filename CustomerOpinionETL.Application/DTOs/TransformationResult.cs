namespace CustomerOpinionETL.Application.DTOs;

public class TransformationResult
{
    public int RecordsTransformed { get; set; }
    public int RecordsSkipped { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}