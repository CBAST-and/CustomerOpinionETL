namespace CustomerOpinionETL.Domain.Entities;

public class Opinion
{
    public int IdOpinion { get; set; }
    public string IdCliente { get; set; } = string.Empty;
    public string IdProducto { get; set; } = string.Empty;
    public int IdFecha { get; set; }
    public DateTime Fecha { get; set; }
    public string? Comentario { get; set; }
    public string? ClasificacionSentimiento { get; set; }
    public decimal? PuntajeSatisfaccion { get; set; }
    public string? CanalOriginal { get; set; }
    public int IdFuente { get; set; }
}