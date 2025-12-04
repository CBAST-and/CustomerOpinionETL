namespace CustomerOpinionETL.Infrastructure.Repositories;

using System.Data;
using Dapper;
using CustomerOpinionETL.Application.Interfaces.Loading;
using CustomerOpinionETL.Domain.Entities;
using Microsoft.Extensions.Logging;

public class ClienteRepository : IClienteRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly ILogger _logger;

    public ClienteRepository(IDbConnection connection, IDbTransaction? transaction, ILogger logger)
    {
        _connection = connection;
        _transaction = transaction;
        _logger = logger;
    }

    public async Task<int> GetOrCreateAsync(Cliente cliente)
    {
        // Verificar si existe
        const string sqlCheck = "SELECT IdCliente FROM DimCliente WHERE IdCliente = @IdCliente";
        var exists = await _connection.QueryFirstOrDefaultAsync<string>(
            sqlCheck,
            new { cliente.IdCliente },
            _transaction);

        if (exists != null)
            return 0; // Ya existe

        // Insertar
        return await UpsertAsync(cliente);
    }

    public async Task<int> UpsertAsync(Cliente cliente)
    {
        const string sql = @"
            MERGE INTO DimCliente AS target
            USING (SELECT @IdCliente AS IdCliente) AS source
            ON target.IdCliente = source.IdCliente
            WHEN MATCHED THEN
                UPDATE SET Nombre = @Nombre, Email = @Email
            WHEN NOT MATCHED THEN
                INSERT (IdCliente, Nombre, Email)
                VALUES (@IdCliente, @Nombre, @Email);";

        return await _connection.ExecuteAsync(sql, cliente, _transaction);
    }
}