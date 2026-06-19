using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EDFCatalogoTablasNet.Models
{
    /// <summary>
    /// Modelo de Usuario para el sistema de autenticación
    /// </summary>
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        public string Password { get; set; } = string.Empty;

        [StringLength(15, ErrorMessage = "El teléfono no puede exceder 15 caracteres")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "La empresa no puede exceder 100 caracteres")]
        public string? Company { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Rol del usuario (Admin, User, Guest)
        /// </summary>
        [StringLength(20, ErrorMessage = "El rol no puede exceder 20 caracteres")]
        public string Role { get; set; } = "User";

        /// <summary>
        /// URL de la imagen de perfil del usuario
        /// </summary>
        [StringLength(500, ErrorMessage = "La URL de la imagen no puede exceder 500 caracteres")]
        public string? ProfileImageUrl { get; set; }

        /// <summary>
        /// Dirección postal del usuario
        /// </summary>
        [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        public string? Address { get; set; }

        /// <summary>
        /// Ocupación o cargo del usuario
        /// </summary>
        [StringLength(100, ErrorMessage = "La ocupación no puede exceder 100 caracteres")]
        public string? Occupation { get; set; }

        /// <summary>Nombre de usuario alternativo al email (coincide con campo Username en MongoDB).</summary>
        [StringLength(100)]
        public string? Username { get; set; }

        [StringLength(200)]
        public string? FullName { get; set; }
    }

    /// <summary>
    /// Modelo para el formulario de login
    /// </summary>
    public class LoginModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    /// <summary>
    /// Modelo para el formulario de registro
    /// </summary>
    public class RegisterModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma tu contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Company { get; set; }

        [Required(ErrorMessage = "Debes aceptar los términos y condiciones")]
        public bool AcceptTerms { get; set; } = false;
    }

    /// <summary>
    /// Modelo para el formulario de contacto
    /// </summary>
    public class ContactModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Company { get; set; }

        [Required(ErrorMessage = "El asunto es obligatorio")]
        [StringLength(200, ErrorMessage = "El asunto no puede exceder 200 caracteres")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es obligatorio")]
        [StringLength(2000, ErrorMessage = "El mensaje no puede exceder 2000 caracteres")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de consulta (General, Soporte, Ventas, etc.)
        /// </summary>
        public string QueryType { get; set; } = "General";
    }
}
