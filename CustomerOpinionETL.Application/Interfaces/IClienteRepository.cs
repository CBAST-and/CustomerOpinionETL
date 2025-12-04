namespace CustomerOpinionETL.Application.Interfaces.Loading;

using CustomerOpinionETL.Domain.Entities;

public interface IClienteRepository
{
    Task<int> GetOrCreateAsync(Cliente cliente);
    Task<int> UpsertAsync(Cliente cliente);
}

public interface IProductoRepository
{
    Task<int> GetOrCreateAsync(Producto producto);
    Task<int> UpsertAsync(Producto producto);
}

public interface IOpinionRepository
{
    Task<int> InsertAsync(Opinion opinion);
    Task<int> InsertBatchAsync(IEnumerable<Opinion> opinions);
    Task<bool> ExistsAsync(string idOriginal, string fuente);
}

public interface IFechaRepository
{
    Task EnsureFechaExistsAsync(DateTime fecha);
    Task<int> GetFechaIdAsync(DateTime fecha);
}

public interface IFuenteRepository
{
    Task<int> GetOrCreateAsync(string nombreFuente);
}