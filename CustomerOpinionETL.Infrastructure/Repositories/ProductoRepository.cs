namespace CustomerOpinionETL.Infrastructure.Repositories;

using System.Data;
using Dapper;
using CustomerOpinionETL.Application.Interfaces.Loading;
using CustomerOpinionETL.Domain.Entities;
using Microsoft.Extensions.Logging;

public class ProductoRepository : IProductoRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly ILogger _logger;

    public ProductoRepository(IDbConnection connection, IDbTransaction? transaction, ILogger logger)
    {
        _connection = connection;
        _transaction = transaction;
        _logger = logger;
    }

    public async Task<int> GetOrCreateAsync(Producto producto)
    {
        const string sqlCheck = "SELECT IdProducto FROM DimProducto WHERE IdProducto = @IdProducto";
        var exists = await _connection.QueryFirstOrDefaultAsync<string>(
            sqlCheck,
            new { producto.IdProducto },
            _transaction);

        if (exists != null)
            return 0;

        return await UpsertAsync(producto);
    }

    public async Task<int> UpsertAsync(Producto producto)
    {
        const string sql = @"
            MERGE INTO DimProducto AS target
            USING (SELECT @IdProducto AS IdProducto) AS source
            ON target.IdProducto = source.IdProducto
            WHEN MATCHED THEN
                UPDATE SET NombreProducto = @NombreProducto, 
                          Categoria = @Categoria,
                          Precio = @Precio
            WHEN NOT MATCHED THEN
                INSERT (IdProducto, NombreProducto, Categoria, Precio)
                VALUES (@IdProducto, @NombreProducto, @Categoria, @Precio);";

        return await _connection.ExecuteAsync(sql, producto, _transaction);
    }
}