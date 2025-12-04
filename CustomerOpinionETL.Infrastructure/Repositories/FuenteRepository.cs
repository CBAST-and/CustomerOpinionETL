namespace CustomerOpinionETL.Infrastructure.Repositories;

using System.Data;
using Dapper;
using CustomerOpinionETL.Application.Interfaces.Loading;
using Microsoft.Extensions.Logging;

public class FuenteRepository : IFuenteRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly ILogger _logger;

    public FuenteRepository(IDbConnection connection, IDbTransaction? transaction, ILogger logger)
    {
        _connection = connection;
        _transaction = transaction;
        _logger = logger;
    }

    public async Task<int> GetOrCreateAsync(string nombreFuente)
    {
        // Buscar si existe
        const string sqlCheck = "SELECT IdFuente FROM DimFuente WHERE NombreFuente = @NombreFuente";
        var id = await _connection.QueryFirstOrDefaultAsync<int?>(
            sqlCheck,
            new { NombreFuente = nombreFuente },
            _transaction);

        if (id.HasValue)
            return id.Value;

        // Insertar y obtener el ID generado
        const string sqlInsert = @"
            INSERT INTO DimFuente (NombreFuente)
            VALUES (@NombreFuente);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        var newId = await _connection.ExecuteScalarAsync<int>(
            sqlInsert,
            new { NombreFuente = nombreFuente },
            _transaction);

        return newId;
    }
}