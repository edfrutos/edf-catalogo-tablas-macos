using EDFCatalogoTablasNet.Models;

namespace EDFCatalogoTablasNet.Services;

/// <summary>
/// Interface para el servicio de catálogos
/// </summary>
public interface ICatalogService
{
    Task<List<Catalog>> GetAllCatalogsAsync(string? userEmail = null, string? userRole = null);
    Task<Catalog?> GetCatalogByIdAsync(string id, string? userEmail = null, string? userRole = null);
    Task<Catalog> CreateCatalogAsync(CreateCatalogRequest request, string userEmail);
    Task<Catalog?> UpdateCatalogAsync(string id, UpdateCatalogRequest request, string? userEmail = null, string? userRole = null);
    Task<bool> DeleteCatalogAsync(string id, string? userEmail = null, string? userRole = null);
    Task<List<Catalog>> SearchCatalogsAsync(string searchTerm, string? userEmail = null, string? userRole = null);

    // Métodos para gestión de filas
    Task<Catalog?> AddRowAsync(string catalogId, CatalogRow catalogRow, string? userEmail = null, string? userRole = null);
    Task<Catalog?> UpdateRowAsync(string catalogId, int rowIndex, CatalogRow catalogRow, string? userEmail = null, string? userRole = null);
    Task<Catalog?> DeleteRowAsync(string catalogId, int rowIndex, string? userEmail = null, string? userRole = null);
}
