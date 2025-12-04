namespace CustomerOpinionETL.Infrastructure.Repositories;

using System.Data;
using CustomerOpinionETL.Application.Interfaces;
using CustomerOpinionETL.Application.Interfaces.Loading;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

public class UnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    // Lazy initialization de repositorios
    private IClienteRepository? _clientes;
    private IProductoRepository? _productos;
    private IOpinionRepository? _opiniones;
    private IFechaRepository? _fechas;
    private IFuenteRepository? _fuentes;

    public UnitOfWork(string connectionString, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _logger = loggerFactory.CreateLogger<UnitOfWork>();
    }

    public IClienteRepository Clientes =>
        _clientes ??= new ClienteRepository(GetConnection(), _transaction, _logger);

    public IProductoRepository Productos =>
        _productos ??= new ProductoRepository(GetConnection(), _transaction, _logger);

    public IOpinionRepository Opiniones =>
        _opiniones ??= new OpinionRepository(GetConnection(), _transaction, _logger);

    public IFechaRepository Fechas =>
        _fechas ??= new FechaRepository(GetConnection(), _transaction, _logger);

    public IFuenteRepository Fuentes =>
        _fuentes ??= new FuenteRepository(GetConnection(), _transaction, _logger);

    private IDbConnection GetConnection()
    {
        if (_connection == null)
        {
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transaction already started");

        _transaction = GetConnection().BeginTransaction();
        _logger.LogDebug("Transaction started");
        await Task.CompletedTask;
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to commit");

        try
        {
            _transaction.Commit();
            _logger.LogDebug("Transaction committed");
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
        await Task.CompletedTask;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to rollback");

        try
        {
            _transaction.Rollback();
            _logger.LogWarning("Transaction rolled back");
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
        await Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        // En este caso, las operaciones se hacen directamente con Dapper
        // Este método puede usarse para operaciones batch futuras
        await Task.CompletedTask;
        return 0;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }
}