namespace EDFCatalogoTablasNet.Models;

/// <summary>
/// Representa una fila individual del catálogo con sus datos y archivos
/// </summary>
public class CatalogRow
{
    /// <summary>
    /// ID único de la fila
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Datos de las columnas (clave-valor)
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Archivos asociados a esta fila específica
    /// </summary>
    public RowFiles Files { get; set; } = new();

    /// <summary>
    /// Fecha de creación de la fila
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Archivos asociados a una fila específica
/// </summary>
public class RowFiles
{
    /// <summary>
    /// Documento asociado a la fila (legacy - se mantiene para compatibilidad)
    /// </summary>
    public string? Document { get; set; }

    /// <summary>
    /// Archivo multimedia asociado a la fila (legacy - se mantiene para compatibilidad)
    /// </summary>
    public string? Multimedia { get; set; }

    /// <summary>
    /// Imagen asociada a la fila (legacy - se mantiene para compatibilidad)
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Lista de documentos (múltiples archivos)
    /// </summary>
    public List<string> Documents { get; set; } = new();

    /// <summary>
    /// Lista de archivos multimedia (múltiples archivos)
    /// </summary>
    public List<string> MultimediaFiles { get; set; } = new();

    /// <summary>
    /// Lista de imágenes (múltiples archivos)
    /// </summary>
    public List<string> Images { get; set; } = new();

    /// <summary>
    /// Lista de archivos adicionales (múltiples por tipo)
    /// </summary>
    public List<RowFileAttachment> AdditionalFiles { get; set; } = new();
}

/// <summary>
/// Archivo adjunto adicional para una fila
/// </summary>
public class RowFileAttachment
{
    /// <summary>
    /// ID único del archivo adjunto
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Nombre del archivo
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// URL o ruta del archivo
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de archivo (document, multimedia, image, other)
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del archivo en bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Descripción del archivo (opcional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha de subida
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
