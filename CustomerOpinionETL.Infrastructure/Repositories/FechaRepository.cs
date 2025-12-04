namespace CustomerOpinionETL.Infrastructure.Repositories;

using System.Data;
using System.Globalization;
using Dapper;
using CustomerOpinionETL.Application.Interfaces.Loading;
using Microsoft.Extensions.Logging;

public class FechaRepository : IFechaRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly ILogger _logger;

    public FechaRepository(IDbConnection connection, IDbTransaction? transaction, ILogger logger)
    {
        _connection = connection;
        _transaction = transaction;
        _logger = logger;
    }

    public async Task EnsureFechaExistsAsync(DateTime fecha)
    {
        var idFecha = GetFechaId(fecha);

        const string sqlCheck = "SELECT IdFecha FROM DimFecha WHERE IdFecha = @IdFecha";
        var exists = await _connection.ExecuteScalarAsync<int?>(
            sqlCheck,
            new { IdFecha = idFecha },
            _transaction);

        if (exists.HasValue)
            return; // Ya existe

        // Insertar
        var trimestre = (fecha.Month - 1) / 3 + 1;
        var nombreMes = fecha.ToString("MMMM", new CultureInfo("es-ES"));

        const string sqlInsert = @"
            INSERT INTO DimFecha (IdFecha, Año, Mes, Trimestre, NombreMes)
            VALUES (@IdFecha, @Año, @Mes, @Trimestre, @NombreMes)";

        await _connection.ExecuteAsync(sqlInsert, new
        {
            IdFecha = idFecha,
            Año = fecha.Year,
            Mes = fecha.Month,
            Trimestre = trimestre,
            NombreMes = nombreMes
        }, _transaction);
    }

    public async Task<int> GetFechaIdAsync(DateTime fecha)
    {
        await EnsureFechaExistsAsync(fecha);
        return GetFechaId(fecha);
    }

    private int GetFechaId(DateTime fecha)
    {
        // Formato YYYYMMDD como entero
        return int.Parse(fecha.ToString("yyyyMMdd"));
    }
}