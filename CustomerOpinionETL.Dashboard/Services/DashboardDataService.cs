namespace CustomerOpinionETL.Dashboard.Services;

using System.Data;
using Dapper;
using CustomerOpinionETL.Dashboard.Models;
using Microsoft.Data.SqlClient;

public interface IDashboardDataService
{
    Task<DashboardViewModel> GetDashboardDataAsync(DashboardFilters? filters = null);
    Task<List<CategoryOption>> GetCategoriesAsync();
    Task<List<SourceOption>> GetSourcesAsync();
}

public class DashboardDataService : IDashboardDataService
{
    private readonly string _connectionString;
    private readonly ILogger<DashboardDataService> _logger;

    public DashboardDataService(
        IConfiguration configuration,
        ILogger<DashboardDataService> logger)
    {
        _connectionString = configuration.GetConnectionString("AnalyticsDb")
            ?? throw new InvalidOperationException("Connection string 'AnalyticsDb' not found");
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<DashboardViewModel> GetDashboardDataAsync(DashboardFilters? filters = null)
    {
        filters ??= new DashboardFilters();

        var viewModel = new DashboardViewModel
        {
            Filters = filters
        };

        try
        {
            using var connection = CreateConnection();

            // Obtener todas las métricas en paralelo
            var tasks = new[]
            {
                GetGeneralStatsAsync(connection, filters),
                GetSentimentDistributionAsync(connection, filters),
                GetMonthlyTrendsAsync(connection, filters),
                GetTopProductsAsync(connection, filters),
                GetSourceStatsAsync(connection, filters),
                GetRecentOpinionsAsync(connection, filters)
            };

            await Task.WhenAll(tasks);

            viewModel.GeneralStats = await tasks[0] as GeneralStats ?? new();
            viewModel.SentimentDistribution = await tasks[1] as List<SentimentDistribution> ?? new();
            viewModel.MonthlyTrends = await tasks[2] as List<MonthlyTrend> ?? new();
            viewModel.TopProducts = await tasks[3] as List<TopProduct> ?? new();
            viewModel.SourceStats = await tasks[4] as List<SourceStats> ?? new();
            viewModel.RecentOpinions = await tasks[5] as List<RecentOpinion> ?? new();

            _logger.LogInformation("Dashboard data loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
        }

        return viewModel;
    }

    private async Task<object> GetGeneralStatsAsync(IDbConnection connection, DashboardFilters filters)
    {
        var sql = @"
            SELECT 
                COUNT(DISTINCT f.IdOpinion) AS TotalOpinions,
                COUNT(DISTINCT f.IdCliente) AS TotalCustomers,
                COUNT(DISTINCT f.IdProducto) AS TotalProducts,
                AVG(f.PuntajeSatisfaccion) AS AverageSatisfaction,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Positiva' THEN 1 ELSE 0 END) AS PositiveOpinions,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Negativa' THEN 1 ELSE 0 END) AS NegativeOpinions,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Neutral' THEN 1 ELSE 0 END) AS NeutralOpinions
            FROM FactOpiniones f
            INNER JOIN DimFecha d ON f.IdFecha = d.IdFecha
            INNER JOIN DimProducto p ON f.IdProducto = p.IdProducto
            INNER JOIN DimFuente s ON f.IdFuente = s.IdFuente
            WHERE 1=1
                {0}";

        var whereClauses = BuildWhereClause(filters);
        sql = string.Format(sql, whereClauses);

        var stats = await connection.QueryFirstOrDefaultAsync<GeneralStats>(sql, filters);

        if (stats != null && stats.TotalOpinions > 0)
        {
            stats.SatisfactionPercentage = (stats.PositiveOpinions * 100m) / stats.TotalOpinions;
        }

        return stats ?? new GeneralStats();
    }

    private async Task<object> GetSentimentDistributionAsync(IDbConnection connection, DashboardFilters filters)
    {
        var sql = @"
            SELECT 
                f.ClasificacionSentimiento AS Clasificacion,
                COUNT(*) AS Cantidad,
                CAST(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM FactOpiniones WHERE 1=1 {0}) AS DECIMAL(5,2)) AS Porcentaje
            FROM FactOpiniones f
            INNER JOIN DimFecha d ON f.IdFecha = d.IdFecha
            INNER JOIN DimProducto p ON f.IdProducto = p.IdProducto
            INNER JOIN DimFuente s ON f.IdFuente = s.IdFuente
            WHERE 1=1 {0}
            GROUP BY f.ClasificacionSentimiento";

        var whereClauses = BuildWhereClause(filters);
        sql = string.Format(sql, whereClauses);

        var distribution = (await connection.QueryAsync<SentimentDistribution>(sql, filters)).ToList();

        // Asignar colores
        foreach (var item in distribution)
        {
            item.Color = item.Clasificacion switch
            {
                "Positiva" => "#10b981",
                "Negativa" => "#ef4444",
                "Neutral" => "#f59e0b",
                _ => "#6b7280"
            };
        }

        return distribution;
    }

    private async Task<object> GetMonthlyTrendsAsync(IDbConnection connection, DashboardFilters filters)
    {
        var sql = @"
            SELECT 
                d.Año,
                d.Mes,
                d.NombreMes AS MesNombre,
                COUNT(f.IdOpinion) AS TotalOpiniones,
                AVG(f.PuntajeSatisfaccion) AS PromedioSatisfaccion,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Positiva' THEN 1 ELSE 0 END) AS Positivas,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Negativa' THEN 1 ELSE 0 END) AS Negativas,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Neutral' THEN 1 ELSE 0 END) AS Neutrales
            FROM FactOpiniones f
            INNER JOIN DimFecha d ON f.IdFecha = d.IdFecha
            INNER JOIN DimProducto p ON f.IdProducto = p.IdProducto
            INNER JOIN DimFuente s ON f.IdFuente = s.IdFuente
            WHERE 1=1 {0}
            GROUP BY d.Año, d.Mes, d.NombreMes
            ORDER BY d.Año, d.Mes";

        var whereClauses = BuildWhereClause(filters);
        sql = string.Format(sql, whereClauses);

        return (await connection.QueryAsync<MonthlyTrend>(sql, filters)).ToList();
    }

    private async Task<object> GetTopProductsAsync(IDbConnection connection, DashboardFilters filters)
    {
        var sql = @"
            SELECT TOP 10
                p.IdProducto,
                p.NombreProducto,
                p.Categoria,
                COUNT(f.IdOpinion) AS TotalOpiniones,
                AVG(f.PuntajeSatisfaccion) AS PromedioSatisfaccion,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Positiva' THEN 1 ELSE 0 END) AS Positivas,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Negativa' THEN 1 ELSE 0 END) AS Negativas,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Neutral' THEN 1 ELSE 0 END) AS Neutrales,
                CAST(SUM(CASE WHEN f.ClasificacionSentimiento = 'Positiva' THEN 1 ELSE 0 END) * 100.0 / COUNT(f.IdOpinion) AS DECIMAL(5,2)) AS PorcentajePositivo
            FROM FactOpiniones f
            INNER JOIN DimProducto p ON f.IdProducto = p.IdProducto
            INNER JOIN DimFecha d ON f.IdFecha = d.IdFecha
            INNER JOIN DimFuente s ON f.IdFuente = s.IdFuente
            WHERE 1=1 {0}
            GROUP BY p.IdProducto, p.NombreProducto, p.Categoria
            HAVING COUNT(f.IdOpinion) >= 3
            ORDER BY AVG(f.PuntajeSatisfaccion) DESC, COUNT(f.IdOpinion) DESC";

        var whereClauses = BuildWhereClause(filters);
        sql = string.Format(sql, whereClauses);

        return (await connection.QueryAsync<TopProduct>(sql, filters)).ToList();
    }

    private async Task<object> GetSourceStatsAsync(IDbConnection connection, DashboardFilters filters)
    {
        var sql = @"
            SELECT 
                s.NombreFuente,
                COUNT(f.IdOpinion) AS TotalOpiniones,
                AVG(f.PuntajeSatisfaccion) AS PromedioSatisfaccion,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Positiva' THEN 1 ELSE 0 END) AS Positivas,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Negativa' THEN 1 ELSE 0 END) AS Negativas,
                SUM(CASE WHEN f.ClasificacionSentimiento = 'Neutral' THEN 1 ELSE 0 END) AS Neutrales
            FROM FactOpiniones f
            INNER JOIN DimFuente s ON f.IdFuente = s.IdFuente
            INNER JOIN DimFecha d ON f.IdFecha = d.IdFecha
            INNER JOIN DimProducto p ON f.IdProducto = p.IdProducto
            WHERE 1=1 {0}
            GROUP BY s.NombreFuente
            ORDER BY COUNT(f.IdOpinion) DESC";

        var whereClauses = BuildWhereClause(filters);
        sql = string.Format(sql, whereClauses);

        var stats = (await connection.QueryAsync<SourceStats>(sql, filters)).ToList();

        // Asignar colores
        var colors = new[] { "#3b82f6", "#8b5cf6", "#ec4899", "#f59e0b", "#10b981" };
        for (int i = 0; i < stats.Count; i++)
        {
            stats[i].Color = colors[i % colors.Length];
        }

        return stats;
    }

    private async Task<object> GetRecentOpinionsAsync(IDbConnection connection, DashboardFilters filters)
    {
        var sql = @"
            SELECT TOP 20
                f.IdOpinion,
                p.NombreProducto,
                p.Categoria,
                f.ClasificacionSentimiento AS Clasificacion,
                f.PuntajeSatisfaccion,
                LEFT(f.Comentario, 200) AS Comentario,
                CAST(CAST(f.IdFecha AS VARCHAR) AS DATE) AS Fecha,
                s.NombreFuente
            FROM FactOpiniones f
            INNER JOIN DimProducto p ON f.IdProducto = p.IdProducto
            INNER JOIN DimFecha d ON f.IdFecha = d.IdFecha
            INNER JOIN DimFuente s ON f.IdFuente = s.IdFuente
            WHERE 1=1 {0}
            ORDER BY f.IdFecha DESC, f.IdOpinion DESC";

        var whereClauses = BuildWhereClause(filters);
        sql = string.Format(sql, whereClauses);

        return (await connection.QueryAsync<RecentOpinion>(sql, filters)).ToList();
    }

    private string BuildWhereClause(DashboardFilters filters)
    {
        var clauses = new List<string>();

        if (filters.StartDate.HasValue)
            clauses.Add($"AND d.IdFecha >= {filters.StartDate.Value:yyyyMMdd}");

        if (filters.EndDate.HasValue)
            clauses.Add($"AND d.IdFecha <= {filters.EndDate.Value:yyyyMMdd}");

        if (!string.IsNullOrEmpty(filters.ProductId))
            clauses.Add("AND p.IdProducto = @ProductId");

        if (!string.IsNullOrEmpty(filters.Categoria))
            clauses.Add("AND p.Categoria = @Categoria");

        if (!string.IsNullOrEmpty(filters.Fuente))
            clauses.Add("AND s.NombreFuente = @Fuente");

        if (!string.IsNullOrEmpty(filters.Clasificacion))
            clauses.Add("AND f.ClasificacionSentimiento = @Clasificacion");

        return string.Join(" ", clauses);
    }

    public async Task<List<CategoryOption>> GetCategoriesAsync()
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT 
                p.Categoria,
                COUNT(DISTINCT f.IdOpinion) AS Count
            FROM FactOpiniones f
            INNER JOIN DimProducto p ON f.IdProducto = p.IdProducto
            WHERE p.Categoria IS NOT NULL
            GROUP BY p.Categoria
            ORDER BY p.Categoria";

        return (await connection.QueryAsync<CategoryOption>(sql)).ToList();
    }

    public async Task<List<SourceOption>> GetSourcesAsync()
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT 
                s.NombreFuente,
                COUNT(f.IdOpinion) AS Count
            FROM FactOpiniones f
            INNER JOIN DimFuente s ON f.IdFuente = s.IdFuente
            GROUP BY s.NombreFuente
            ORDER BY s.NombreFuente";

        return (await connection.QueryAsync<SourceOption>(sql)).ToList();
    }
}