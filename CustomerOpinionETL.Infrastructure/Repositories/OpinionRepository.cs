namespace CustomerOpinionETL.Infrastructure.Repositories;

using System.Data;
using Dapper;
using CustomerOpinionETL.Application.Interfaces.Loading;
using CustomerOpinionETL.Domain.Entities;
using Microsoft.Extensions.Logging;

public class OpinionRepository : IOpinionRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly ILogger _logger;

    public OpinionRepository(IDbConnection connection, IDbTransaction? transaction, ILogger logger)
    {
        _connection = connection;
        _transaction = transaction;
        _logger = logger;
    }

    public async Task<int> InsertAsync(Opinion opinion)
    {
        const string sql = @"
            INSERT INTO FactOpiniones 
                (IdCliente, IdProducto, IdFecha, IdFuente, 
                 ClasificacionSentimiento, PuntajeSatisfaccion, 
                 Comentario, CanalOriginal)
            VALUES 
                (@IdCliente, @IdProducto, @IdFecha, @IdFuente, 
                 @ClasificacionSentimiento, @PuntajeSatisfaccion, 
                 @Comentario, @CanalOriginal)";

        return await _connection.ExecuteAsync(sql, opinion, _transaction);
    }

    public async Task<int> InsertBatchAsync(IEnumerable<Opinion> opinions)
    {
        const string sql = @"
            INSERT INTO FactOpiniones 
                (IdCliente, IdProducto, IdFecha, IdFuente, 
                 ClasificacionSentimiento, PuntajeSatisfaccion, 
                 Comentario, CanalOriginal)
            VALUES 
                (@IdCliente, @IdProducto, @IdFecha, @IdFuente, 
                 @ClasificacionSentimiento, @PuntajeSatisfaccion, 
                 @Comentario, @CanalOriginal)";

        return await _connection.ExecuteAsync(sql, opinions, _transaction);
    }

    public async Task<bool> ExistsAsync(string idOriginal, string fuente)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM FactOpiniones 
            WHERE CanalOriginal = @Fuente";

        var count = await _connection.ExecuteScalarAsync<int>(
            sql,
            new { Fuente = fuente },
            _transaction);

        return count > 0;
    }
}