using EDFCatalogoTablasNet.Models;
using MongoDB.Driver;

namespace EDFCatalogoTablasNet.Services;

/// <summary>
/// Servicio de catálogos usando MongoDB
/// </summary>
public class CatalogServiceMongo : ICatalogService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<CatalogServiceMongo> _logger;

    public CatalogServiceMongo(MongoDbContext context, ILogger<CatalogServiceMongo> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Catalog>> GetAllCatalogsAsync(string? userEmail = null, string? userRole = null)
    {
        _logger.LogInformation("[GetAllCatalogsAsync] Iniciando consulta - UserEmail: '{Email}', UserRole: '{Role}'", userEmail ?? "NULL", userRole ?? "NULL");

        FilterDefinition<Catalog> filter;

        // Si es admin, puede ver todos los catálogos
        if (userRole == "Admin")
        {
            filter = Builders<Catalog>.Filter.Empty;
            _logger.LogInformation("[GetAllCatalogsAsync] Filtro aplicado: Admin - Ver todos los catálogos");
        }
        // Si es usuario normal, solo puede ver sus propios catálogos
        else if (!string.IsNullOrEmpty(userEmail))
        {
            filter = Builders<Catalog>.Filter.Eq(c => c.CreatedBy, userEmail);
            _logger.LogInformation("[GetAllCatalogsAsync] Filtro aplicado: Usuario normal - Solo catálogos de '{Email}'", userEmail);
        }
        // Si no hay información del usuario, no mostrar nada
        else
        {
            filter = Builders<Catalog>.Filter.Eq(c => c.Id, "nonexistent");
            _logger.LogWarning("[GetAllCatalogsAsync] Filtro aplicado: Sin usuario - No mostrar nada");
        }

        var catalogs = await _context.Catalogs
            .Find(filter)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync();

        _logger.LogInformation("[GetAllCatalogsAsync] Catálogos encontrados: {Count}", catalogs.Count);

        // Log de los primeros 3 catálogos para debug
        foreach (var catalog in catalogs.Take(3))
        {
            _logger.LogInformation("[GetAllCatalogsAsync] Catálogo: ID={Id}, Name='{Name}', CreatedBy='{CreatedBy}', CreatedAt={CreatedAt}",
                catalog.Id, catalog.Name, catalog.CreatedBy, catalog.CreatedAt);
        }

        return catalogs;
    }

    public async Task<Catalog?> GetCatalogByIdAsync(string id, string? userEmail = null, string? userRole = null)
    {
        FilterDefinition<Catalog> filter;

        // Si es admin, puede ver cualquier catálogo
        if (userRole == "Admin")
        {
            filter = Builders<Catalog>.Filter.Eq(c => c.Id, id);
        }
        // Si es usuario normal, solo puede ver sus propios catálogos
        else if (!string.IsNullOrEmpty(userEmail))
        {
            filter = Builders<Catalog>.Filter.And(
                Builders<Catalog>.Filter.Eq(c => c.Id, id),
                Builders<Catalog>.Filter.Eq(c => c.CreatedBy, userEmail)
            );
        }
        // Si no hay información del usuario, no mostrar nada
        else
        {
            return null;
        }

        return await _context.Catalogs
            .Find(filter)
            .FirstOrDefaultAsync();
    }

    public async Task<Catalog> CreateCatalogAsync(CreateCatalogRequest request, string userEmail)
    {
        try
        {
            _logger.LogInformation("[CreateCatalogAsync] Iniciando creación - Name: '{Name}', User: '{User}'", request.Name, userEmail);
            _logger.LogInformation("[CreateCatalogAsync] DocumentoUrl: '{Doc}', MultimediaUrl: '{Mult}', ImagenUrl: '{Img}'",
                request.DocumentoUrl ?? "NULL", request.MultimediaUrl ?? "NULL", request.ImagenUrl ?? "NULL");

            var catalog = new Catalog
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                Fecha = request.Fecha,
                DocumentoUrl = request.DocumentoUrl,
                MultimediaUrl = request.MultimediaUrl,
                ImagenUrl = request.ImagenUrl,
                Headers = request.Headers,
                Rows = new List<CatalogRow>(), // Inicializar siempre como lista vacía
                CreatedBy = userEmail,
                Owner = userEmail,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Catalogs.InsertOneAsync(catalog);
            _logger.LogInformation("[CreateCatalogAsync] Catálogo creado exitosamente: {Name} con ID {Id}", catalog.Name, catalog.Id);

            return catalog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateCatalogAsync] Error al crear catálogo - Name: '{Name}', User: '{User}'", request.Name, userEmail);
            throw;
        }
    }

    public async Task<Catalog?> UpdateCatalogAsync(string id, UpdateCatalogRequest request, string? userEmail = null, string? userRole = null)
    {
        FilterDefinition<Catalog> filter;

        // Si es admin, puede actualizar cualquier catálogo
        if (userRole == "Admin")
        {
            filter = Builders<Catalog>.Filter.Eq(c => c.Id, id);
        }
        // Si es usuario normal, solo puede actualizar sus propios catálogos
        else if (!string.IsNullOrEmpty(userEmail))
        {
            filter = Builders<Catalog>.Filter.And(
                Builders<Catalog>.Filter.Eq(c => c.Id, id),
                Builders<Catalog>.Filter.Eq(c => c.CreatedBy, userEmail)
            );
        }
        // Si no hay información del usuario, no permitir actualización
        else
        {
            return null;
        }

        var catalog = await _context.Catalogs
            .Find(filter)
            .FirstOrDefaultAsync();

        if (catalog == null)
            return null;

        catalog.Name = request.Name;
        catalog.Description = request.Description;
        catalog.Category = request.Category;
        catalog.Fecha = request.Fecha;
        catalog.ImagenUrl = request.ImagenUrl;
        catalog.Headers = request.Headers;
        catalog.UpdatedAt = DateTime.UtcNow;

        await _context.Catalogs.ReplaceOneAsync(c => c.Id == id, catalog);
        _logger.LogInformation("Catálogo actualizado: {Name} con ID {Id}", catalog.Name, catalog.Id);

        return catalog;
    }

    public async Task<bool> DeleteCatalogAsync(string id, string? userEmail = null, string? userRole = null)
    {
        FilterDefinition<Catalog> filter;

        // Si es admin, puede eliminar cualquier catálogo
        if (userRole == "Admin")
        {
            filter = Builders<Catalog>.Filter.Eq(c => c.Id, id);
        }
        // Si es usuario normal, solo puede eliminar sus propios catálogos
        else if (!string.IsNullOrEmpty(userEmail))
        {
            filter = Builders<Catalog>.Filter.And(
                Builders<Catalog>.Filter.Eq(c => c.Id, id),
                Builders<Catalog>.Filter.Eq(c => c.CreatedBy, userEmail)
            );
        }
        // Si no hay información del usuario, no permitir eliminación
        else
        {
            return false;
        }

        var result = await _context.Catalogs.DeleteOneAsync(filter);

        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Catálogo eliminado con ID {Id}", id);
            return true;
        }

        return false;
    }

    public async Task<List<Catalog>> SearchCatalogsAsync(string searchTerm, string? userEmail = null, string? userRole = null)
    {
        FilterDefinition<Catalog> userFilter;

        // Si es admin, puede buscar en todos los catálogos
        if (userRole == "Admin")
        {
            userFilter = Builders<Catalog>.Filter.Empty;
        }
        // Si es usuario normal, solo puede buscar en sus propios catálogos
        else if (!string.IsNullOrEmpty(userEmail))
        {
            userFilter = Builders<Catalog>.Filter.Eq(c => c.CreatedBy, userEmail);
        }
        // Si no hay información del usuario, no mostrar nada
        else
        {
            userFilter = Builders<Catalog>.Filter.Eq(c => c.Id, "nonexistent");
        }

        var searchFilter = Builders<Catalog>.Filter.Or(
            Builders<Catalog>.Filter.Regex(c => c.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
            Builders<Catalog>.Filter.Regex(c => c.Description, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
        );

        var combinedFilter = Builders<Catalog>.Filter.And(userFilter, searchFilter);

        return await _context.Catalogs
            .Find(combinedFilter)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Catalog?> AddRowAsync(string catalogId, CatalogRow catalogRow, string? userEmail = null, string? userRole = null)
    {
        FilterDefinition<Catalog> filter;

        // Si es admin, puede agregar filas a cualquier catálogo
        if (userRole == "Admin")
        {
            filter = Builders<Catalog>.Filter.Eq(c => c.Id, catalogId);
        }
        // Si es usuario normal, solo puede agregar filas a sus propios catálogos
        else if (!string.IsNullOrEmpty(userEmail))
        {
            filter = Builders<Catalog>.Filter.And(
                Builders<Catalog>.Filter.Eq(c => c.Id, catalogId),
                Builders<Catalog>.Filter.Eq(c => c.CreatedBy, userEmail)
            );
        }
        // Si no hay información del usuario, no permitir
        else
        {
            return null;
        }

        var catalog = await _context.Catalogs
            .Find(filter)
            .FirstOrDefaultAsync();

        if (catalog == null)
            return null;

        // Inicializar Rows si es null (catálogos legacy)
        if (catalog.Rows == null)
        {
            catalog.Rows = new List<CatalogRow>();
        }

        // Validar que los datos de la fila coincidan con las cabeceras
        if (catalog.Headers.Length > 0 && !catalog.Headers.All(h => catalogRow.Data.ContainsKey(h)))
        {
            _logger.LogWarning("Las columnas de la fila no coinciden con las cabeceras del catálogo {Id}", catalogId);
            return null;
        }

        // Asignar timestamps si no están establecidos
        if (catalogRow.CreatedAt == default)
            catalogRow.CreatedAt = DateTime.UtcNow;
        if (catalogRow.UpdatedAt == default)
            catalogRow.UpdatedAt = DateTime.UtcNow;

        catalog.Rows.Add(catalogRow);
        catalog.UpdatedAt = DateTime.UtcNow;

        await _context.Catalogs.ReplaceOneAsync(c => c.Id == catalogId, catalog);
        _logger.LogInformation("Fila agregada al catálogo {Name} con ID {Id}", catalog.Name, catalog.Id);

        return catalog;
    }

    public async Task<Catalog?> UpdateRowAsync(string catalogId, int rowIndex, CatalogRow catalogRow, string? userEmail = null, string? userRole = null)
    {
        FilterDefinition<Catalog> filter;

        // Si es admin, puede actualizar filas en cualquier catálogo
        if (userRole == "Admin")
        {
            filter = Builders<Catalog>.Filter.Eq(c => c.Id, catalogId);
        }
        // Si es usuario normal, solo puede actualizar filas en sus propios catálogos
        else if (!string.IsNullOrEmpty(userEmail))
        {
            filter = Builders<Catalog>.Filter.And(
                Builders<Catalog>.Filter.Eq(c => c.Id, catalogId),
                Builders<Catalog>.Filter.Eq(c => c.CreatedBy, userEmail)
            );
        }
        // Si no hay información del usuario, no permitir
        else
        {
            return null;
        }

        var catalog = await _context.Catalogs
            .Find(filter)
            .FirstOrDefaultAsync();

        if (catalog == null)
            return null;

        // Inicializar Rows si es null (catálogos legacy)
        if (catalog.Rows == null)
        {
            catalog.Rows = new List<CatalogRow>();
        }

        if (rowIndex < 0 || rowIndex >= catalog.Rows.Count)
        {
            _logger.LogWarning("Índice de fila fuera de rango: {Index} para catálogo {Id}", rowIndex, catalogId);
            return null;
        }

        catalog.Rows[rowIndex] = catalogRow;
        catalog.Rows[rowIndex].UpdatedAt = DateTime.UtcNow;
        catalog.UpdatedAt = DateTime.UtcNow;

        await _context.Catalogs.ReplaceOneAsync(c => c.Id == catalogId, catalog);
        _logger.LogInformation("Fila {Index} actualizada en catálogo {Name} con ID {Id}", rowIndex, catalog.Name, catalog.Id);

        return catalog;
    }

    public async Task<Catalog?> DeleteRowAsync(string catalogId, int rowIndex, string? userEmail = null, string? userRole = null)
    {
        FilterDefinition<Catalog> filter;

        // Si es admin, puede eliminar filas en cualquier catálogo
        if (userRole == "Admin")
        {
            filter = Builders<Catalog>.Filter.Eq(c => c.Id, catalogId);
        }
        // Si es usuario normal, solo puede eliminar filas en sus propios catálogos
        else if (!string.IsNullOrEmpty(userEmail))
        {
            filter = Builders<Catalog>.Filter.And(
                Builders<Catalog>.Filter.Eq(c => c.Id, catalogId),
                Builders<Catalog>.Filter.Eq(c => c.CreatedBy, userEmail)
            );
        }
        // Si no hay información del usuario, no permitir
        else
        {
            return null;
        }

        var catalog = await _context.Catalogs
            .Find(filter)
            .FirstOrDefaultAsync();

        if (catalog == null)
            return null;

        // Inicializar Rows si es null (catálogos legacy)
        if (catalog.Rows == null)
        {
            catalog.Rows = new List<CatalogRow>();
        }

        if (rowIndex < 0 || rowIndex >= catalog.Rows.Count)
        {
            _logger.LogWarning("Índice de fila fuera de rango: {Index} para catálogo {Id}", rowIndex, catalogId);
            return null;
        }

        catalog.Rows.RemoveAt(rowIndex);
        catalog.UpdatedAt = DateTime.UtcNow;

        await _context.Catalogs.ReplaceOneAsync(c => c.Id == catalogId, catalog);
        _logger.LogInformation("Fila {Index} eliminada del catálogo {Name} con ID {Id}", rowIndex, catalog.Name, catalog.Id);

        return catalog;
    }
}

