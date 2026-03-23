using System.ComponentModel.DataAnnotations;

namespace EDFCatalogoTablasNet.Models
{
    /// <summary>
    /// Modelo para actualizar un catálogo existente
    /// </summary>
    public class UpdateCatalogRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 500 caracteres")]
        public string Description { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "La categoría no puede exceder 50 caracteres")]
        public string? Category { get; set; }

        public DateTime? Fecha { get; set; }

        /// <summary>
        /// URL de la imagen identificativa del catálogo
        /// </summary>
        public string? ImagenUrl { get; set; }

        public string[] Headers { get; set; } = [];
    }
}

