namespace CustomerOpinionETL.Application.UseCases;

using CustomerOpinionETL.Application.Interfaces;
using CustomerOpinionETL.Domain.Entities;
using Microsoft.Extensions.Logging;

public class LoadDimensionsUseCase
{
    private readonly ILogger<LoadDimensionsUseCase> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public LoadDimensionsUseCase(
        ILogger<LoadDimensionsUseCase> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> LoadClientesAsync(IEnumerable<Cliente> clientes)
    {
        _logger.LogInformation("Loading Clientes dimension...");
        var count = 0;

        foreach (var cliente in clientes)
        {
            try
            {
                await _unitOfWork.Clientes.UpsertAsync(cliente);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cliente: {IdCliente}", cliente.IdCliente);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("✓ Loaded {Count} clientes", count);
        return count;
    }

    public async Task<int> LoadProductosAsync(IEnumerable<Producto> productos)
    {
        _logger.LogInformation("Loading Productos dimension...");
        var count = 0;

        foreach (var producto in productos)
        {
            try
            {
                await _unitOfWork.Productos.UpsertAsync(producto);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading producto: {IdProducto}", producto.IdProducto);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("✓ Loaded {Count} productos", count);
        return count;
    }
}