using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EDFCatalogoTablasNet.Models;

/// <summary>
/// Modelo para representar un catálogo en el sistema
/// </summary>
public class Catalog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Category { get; set; }

    // Nuevos campos multimedia
    public DateTime? Fecha { get; set; }

    public string? DocumentoUrl { get; set; }

    public string? MultimediaUrl { get; set; }

    public string? ImagenUrl { get; set; }

    public string[] Headers { get; set; } = [];

    /// <summary>
    /// Filas del catálogo con sus datos y archivos asociados
    /// </summary>
    [BsonIgnoreIfNull]
    public List<CatalogRow>? Rows { get; set; }

    /// <summary>
    /// Filas del catálogo en formato legacy (para compatibilidad con datos antiguos)
    /// Este campo se usa automáticamente si Rows no está disponible en MongoDB
    /// </summary>
    [BsonIgnoreIfNull]
    [Obsolete("Usar Rows en su lugar")]
    public List<Dictionary<string, object>>? LegacyRows { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? Miniatura { get; set; }

    public int NumRows => (Rows?.Count ?? 0) + (LegacyRows?.Count ?? 0);
}

/// <summary>
/// Modelo para crear o actualizar un catálogo
/// </summary>
public class CreateCatalogRequest
{
    [Required(ErrorMessage = "El nombre del catálogo es obligatorio")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 1000 caracteres")]
    public string Description { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "La categoría no puede exceder los 100 caracteres")]
    public string? Category { get; set; }

    // Nuevos campos multimedia
    public DateTime? Fecha { get; set; } = DateTime.Today;

    public string? DocumentoUrl { get; set; }

    public string? MultimediaUrl { get; set; }

    public string? ImagenUrl { get; set; }

    public string[] Headers { get; set; } = [];
}

/// <summary>
/// Modelo para respuesta de API de catálogos
/// </summary>
public class CatalogResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Catalog? Data { get; set; }
    public List<Catalog>? Catalogs { get; set; }
}
