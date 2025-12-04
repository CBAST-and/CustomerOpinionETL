namespace CustomerOpinionETL.Domain.Entities;

public class Producto
{
    public string IdProducto { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public decimal? Precio { get; set; }
}
