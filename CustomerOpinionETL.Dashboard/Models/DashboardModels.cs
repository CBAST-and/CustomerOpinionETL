namespace CustomerOpinionETL.Dashboard.Models;

/// <summary>
/// Modelo principal del dashboard con todas las métricas
/// </summary>
public class DashboardViewModel
{
    public GeneralStats GeneralStats { get; set; } = new();
    public List<SentimentDistribution> SentimentDistribution { get; set; } = new();
    public List<MonthlyTrend> MonthlyTrends { get; set; } = new();
    public List<TopProduct> TopProducts { get; set; } = new();
    public List<SourceStats> SourceStats { get; set; } = new();
    public List<RecentOpinion> RecentOpinions { get; set; } = new();

    // Filtros aplicados
    public DashboardFilters Filters { get; set; } = new();
}

/// <summary>
/// Estadísticas generales
/// </summary>
public class GeneralStats
{
    public int TotalOpinions { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public decimal AverageSatisfaction { get; set; }
    public decimal SatisfactionPercentage { get; set; }
    public int PositiveOpinions { get; set; }
    public int NegativeOpinions { get; set; }
    public int NeutralOpinions { get; set; }
}

/// <summary>
/// Distribución de sentimientos para gráfico de pastel
/// </summary>
public class SentimentDistribution
{
    public string Clasificacion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Porcentaje { get; set; }
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Tendencia mensual para gráfico de línea
/// </summary>
public class MonthlyTrend
{
    public int Año { get; set; }
    public int Mes { get; set; }
    public string MesNombre { get; set; } = string.Empty;
    public int TotalOpiniones { get; set; }
    public decimal PromedioSatisfaccion { get; set; }
    public int Positivas { get; set; }
    public int Negativas { get; set; }
    public int Neutrales { get; set; }
}

/// <summary>
/// Top productos por satisfacción
/// </summary>
public class TopProduct
{
    public string IdProducto { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int TotalOpiniones { get; set; }
    public decimal PromedioSatisfaccion { get; set; }
    public int Positivas { get; set; }
    public int Negativas { get; set; }
    public int Neutrales { get; set; }
    public decimal PorcentajePositivo { get; set; }
}

/// <summary>
/// Estadísticas por fuente de datos
/// </summary>
public class SourceStats
{
    public string NombreFuente { get; set; } = string.Empty;
    public int TotalOpiniones { get; set; }
    public decimal PromedioSatisfaccion { get; set; }
    public int Positivas { get; set; }
    public int Negativas { get; set; }
    public int Neutrales { get; set; }
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Opiniones recientes
/// </summary>
public class RecentOpinion
{
    public int IdOpinion { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string Clasificacion { get; set; } = string.Empty;
    public decimal? PuntajeSatisfaccion { get; set; }
    public string? Comentario { get; set; }
    public DateTime Fecha { get; set; }
    public string NombreFuente { get; set; } = string.Empty;
}

/// <summary>
/// Filtros del dashboard
/// </summary>
public class DashboardFilters
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ProductId { get; set; }
    public string? Categoria { get; set; }
    public string? Fuente { get; set; }
    public string? Clasificacion { get; set; }
}

/// <summary>
/// Lista de categorías para filtro
/// </summary>
public class CategoryOption
{
    public string Categoria { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Lista de fuentes para filtro
/// </summary>
public class SourceOption
{
    public string NombreFuente { get; set; } = string.Empty;
    public int Count { get; set; }
}