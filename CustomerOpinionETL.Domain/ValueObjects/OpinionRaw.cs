namespace CustomerOpinionETL.Domain.ValueObjects;

public class OpinionRaw
{
    public string IdOriginal { get; set; } = string.Empty;
    public string? ClienteIdRaw { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteEmail { get; set; }
    public string? ProductoIdRaw { get; set; }
    public string? ProductoNombre { get; set; }
    public string? Categoria { get; set; }
    public string? FechaRaw { get; set; }
    public string? ComentarioRaw { get; set; }
    public string? RatingRaw { get; set; }
    public string? ClasificacionRaw { get; set; }
    public string FuenteOrigen { get; set; } = string.Empty;
    public Dictionary<string, string> MetadataAdicional { get; set; } = new();
}