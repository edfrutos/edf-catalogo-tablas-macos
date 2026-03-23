using Microsoft.AspNetCore.Mvc;
using EDFCatalogoTablasNet.Models;
using EDFCatalogoTablasNet.Services;

namespace EDFCatalogoTablasNet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly IAuthService _authService;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(ICatalogService catalogService, IAuthService authService, ILogger<CatalogController> logger)
    {
        _catalogService = catalogService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Obtener todos los catálogos (filtrados por usuario)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CatalogResponse>> GetCatalogs()
    {
        try
        {
            var userEmail = _authService.CurrentUserEmail;
            var userRole = _authService.CurrentUserRole;

            var catalogs = await _catalogService.GetAllCatalogsAsync(userEmail, userRole);
            return Ok(new CatalogResponse
            {
                Success = true,
                Message = "Catálogos obtenidos correctamente",
                Catalogs = catalogs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener catálogos");
            return StatusCode(500, new CatalogResponse
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Obtener un catálogo por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CatalogResponse>> GetCatalog(string id)
    {
        try
        {
            var userEmail = _authService.CurrentUserEmail;
            var userRole = _authService.CurrentUserRole;

            var catalog = await _catalogService.GetCatalogByIdAsync(id, userEmail, userRole);
            if (catalog == null)
            {
                return NotFound(new CatalogResponse
                {
                    Success = false,
                    Message = "Catálogo no encontrado o no tienes permisos para verlo"
                });
            }

            return Ok(new CatalogResponse
            {
                Success = true,
                Message = "Catálogo obtenido correctamente",
                Data = catalog
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener catálogo {Id}", id);
            return StatusCode(500, new CatalogResponse
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Crear un nuevo catálogo
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CatalogResponse>> CreateCatalog([FromBody] CreateCatalogRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new CatalogResponse
                {
                    Success = false,
                    Message = "Datos de entrada inválidos"
                });
            }

            var userEmail = _authService.CurrentUserEmail;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new CatalogResponse
                {
                    Success = false,
                    Message = "Debes iniciar sesión para crear catálogos"
                });
            }

            var catalog = await _catalogService.CreateCatalogAsync(request, userEmail);
            return CreatedAtAction(nameof(GetCatalog), new { id = catalog.Id }, new CatalogResponse
            {
                Success = true,
                Message = "Catálogo creado correctamente",
                Data = catalog
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear catálogo");
            return StatusCode(500, new CatalogResponse
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Actualizar un catálogo existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CatalogResponse>> UpdateCatalog(string id, [FromBody] UpdateCatalogRequest request)
    {
        try
        {
            var userEmail = _authService.CurrentUserEmail;
            var userRole = _authService.CurrentUserRole;

            var catalog = await _catalogService.UpdateCatalogAsync(id, request, userEmail, userRole);
            if (catalog == null)
            {
                return NotFound(new CatalogResponse
                {
                    Success = false,
                    Message = "Catálogo no encontrado o no tienes permisos para editarlo"
                });
            }

            return Ok(new CatalogResponse
            {
                Success = true,
                Message = "Catálogo actualizado correctamente",
                Data = catalog
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar catálogo {Id}", id);
            return StatusCode(500, new CatalogResponse
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Eliminar un catálogo
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<CatalogResponse>> DeleteCatalog(string id)
    {
        try
        {
            var userEmail = _authService.CurrentUserEmail;
            var userRole = _authService.CurrentUserRole;

            var success = await _catalogService.DeleteCatalogAsync(id, userEmail, userRole);
            if (!success)
            {
                return NotFound(new CatalogResponse
                {
                    Success = false,
                    Message = "Catálogo no encontrado o no tienes permisos para eliminarlo"
                });
            }

            return Ok(new CatalogResponse
            {
                Success = true,
                Message = "Catálogo eliminado correctamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar catálogo {Id}", id);
            return StatusCode(500, new CatalogResponse
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }
}
