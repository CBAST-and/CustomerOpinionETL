namespace CustomerOpinionETL.Domain.Entities;

public class Cliente
{
    public string IdCliente { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
}
