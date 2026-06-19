using EDFCatalogoTablasNet;
using EDFCatalogoTablasNet.Models;
using EDFCatalogoTablasNet.Serialization;
using MongoDB.Bson;
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

        if (RoleHelper.IsAdmin(userRole))
            _logger.LogInformation("[GetAllCatalogsAsync] Filtro aplicado: Admin - Ver todos los catálogos");
        else if (!string.IsNullOrEmpty(userEmail))
            _logger.LogInformation("[GetAllCatalogsAsync] Filtro aplicado: Usuario normal - Propietario/createdBy (regex) '{Email}'", userEmail);
        else
            _logger.LogWarning("[GetAllCatalogsAsync] Filtro aplicado: Sin usuario - No mostrar nada");

        var access = CatalogBsonDeserializer.BuildAccessFilter(userEmail, userRole);
        var docs = await _context.CatalogsBson.Find(access).ToListAsync();

        var catalogs = new List<Catalog>();
        foreach (var doc in docs)
        {
            var c = CatalogBsonDeserializer.TryDeserialize(doc, _logger);
            if (c != null)
                catalogs.Add(c);
        }

        catalogs.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        _logger.LogInformation("[GetAllCatalogsAsync] Catálogos encontrados: {Count}", catalogs.Count);

        foreach (var catalog in catalogs.Take(3))
        {
            _logger.LogInformation("[GetAllCatalogsAsync] Catálogo: ID={Id}, Name='{Name}', CreatedBy='{CreatedBy}', CreatedAt={CreatedAt}",
                catalog.Id, catalog.Name, catalog.CreatedBy, catalog.CreatedAt);
        }

        return catalogs;
    }

    public async Task<Catalog?> GetCatalogByIdAsync(string id, string? userEmail = null, string? userRole = null) =>
        await FindCatalogForWriteAsync(id, userEmail, userRole);

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
        var catalog = await FindCatalogForWriteAsync(id, userEmail, userRole);

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
        if (!RoleHelper.IsAdmin(userRole) && string.IsNullOrEmpty(userEmail))
            return false;

        var bsonFilter = CatalogBsonDeserializer.BuildIdFilter(id);
        if (!RoleHelper.IsAdmin(userRole))
        {
            if (string.IsNullOrEmpty(userEmail))
                return false;
            bsonFilter = Builders<BsonDocument>.Filter.And(
                bsonFilter, CatalogBsonDeserializer.BuildUserOwnershipFilter(userEmail));
        }

        var result = await _context.CatalogsBson.DeleteOneAsync(bsonFilter);

        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Catálogo eliminado con ID {Id}", id);
            return true;
        }

        return false;
    }

    public async Task<List<Catalog>> SearchCatalogsAsync(string searchTerm, string? userEmail = null, string? userRole = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllCatalogsAsync(userEmail, userRole);

        var term = searchTerm.Trim();
        var all = await GetAllCatalogsAsync(userEmail, userRole);
        return all.Where(c =>
            c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(c.Description) && c.Description.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(c.Category) && c.Category.Contains(term, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    public async Task<Catalog?> AddRowAsync(string catalogId, CatalogRow catalogRow, string? userEmail = null, string? userRole = null)
    {
        var catalog = await FindCatalogForWriteAsync(catalogId, userEmail, userRole);

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
        var catalog = await FindCatalogForWriteAsync(catalogId, userEmail, userRole);

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
        var catalog = await FindCatalogForWriteAsync(catalogId, userEmail, userRole);

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

    private async Task<Catalog?> FindCatalogForWriteAsync(string catalogId, string? userEmail, string? userRole)
    {
        var f = CatalogBsonDeserializer.BuildIdFilter(catalogId);
        if (!RoleHelper.IsAdmin(userRole))
        {
            if (string.IsNullOrEmpty(userEmail))
                return null;
            f = Builders<BsonDocument>.Filter.And(f, CatalogBsonDeserializer.BuildUserOwnershipFilter(userEmail));
        }

        var doc = await _context.CatalogsBson.Find(f).FirstOrDefaultAsync();
        return doc == null ? null : CatalogBsonDeserializer.TryDeserialize(doc, _logger);
    }
}

