namespace CustomerOpinionETL.Domain.Entities;

public class FuenteDatos
{
    public int IdFuente { get; set; }
    public string NombreFuente { get; set; } = string.Empty;
    public TipoFuente Tipo { get; set; }
}

public enum TipoFuente
{
    CSV,
    BaseDatos,
    ApiRest
}