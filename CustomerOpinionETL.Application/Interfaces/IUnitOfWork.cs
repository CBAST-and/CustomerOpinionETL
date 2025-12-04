namespace CustomerOpinionETL.Application.Interfaces;

using CustomerOpinionETL.Application.Interfaces.Loading;

public interface IUnitOfWork : IDisposable
{
    IClienteRepository Clientes { get; }
    IProductoRepository Productos { get; }
    IOpinionRepository Opiniones { get; }
    IFechaRepository Fechas { get; }
    IFuenteRepository Fuentes { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}