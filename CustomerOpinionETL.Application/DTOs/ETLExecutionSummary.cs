namespace CustomerOpinionETL.Application.DTOs;

public class ETLExecutionSummary
{
    public DateTime ExecutionStartTime { get; set; }
    public DateTime ExecutionEndTime { get; set; }
    public TimeSpan TotalDuration => ExecutionEndTime - ExecutionStartTime;

    public ExtractionResult? CsvExtraction { get; set; }
    public ExtractionResult? DatabaseExtraction { get; set; }
    public ExtractionResult? ApiExtraction { get; set; }

    public TransformationResult? Transformation { get; set; }
    public LoadingResult? Loading { get; set; }

    public int TotalRecordsProcessed { get; set; }
    public bool Success { get; set; }

    public string GetSummary()
    {
        return $@"
========================================
ETL EXECUTION SUMMARY
========================================
Start Time: {ExecutionStartTime:yyyy-MM-dd HH:mm:ss}
End Time: {ExecutionEndTime:yyyy-MM-dd HH:mm:ss}
Total Duration: {TotalDuration}

EXTRACTION:
  CSV: {CsvExtraction?.RecordsExtracted ?? 0} records ({CsvExtraction?.Duration})
  Database: {DatabaseExtraction?.RecordsExtracted ?? 0} records ({DatabaseExtraction?.Duration})
  API: {ApiExtraction?.RecordsExtracted ?? 0} records ({ApiExtraction?.Duration})

TRANSFORMATION:
  Transformed: {Transformation?.RecordsTransformed ?? 0}
  Skipped: {Transformation?.RecordsSkipped ?? 0}
  Duration: {Transformation?.Duration}

LOADING:
  Loaded: {Loading?.RecordsLoaded ?? 0}
  Failed: {Loading?.RecordsFailed ?? 0}
  Duration: {Loading?.Duration}

Total Records Processed: {TotalRecordsProcessed}
Status: {(Success ? "SUCCESS" : "FAILED")}
========================================";
    }
}