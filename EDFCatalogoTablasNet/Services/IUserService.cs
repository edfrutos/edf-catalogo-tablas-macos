using EDFCatalogoTablasNet.Models;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Interfaz para el servicio de gestión de usuarios
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Obtener todos los usuarios del sistema
        /// </summary>
        Task<List<User>> GetAllUsersAsync();

        /// <summary>
        /// Obtener usuario por ID
        /// </summary>
        Task<User?> GetUserByIdAsync(string id);

        /// <summary>
        /// Obtener usuario por email
        /// </summary>
        Task<User?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Actualizar rol de un usuario
        /// </summary>
        Task<bool> UpdateUserRoleAsync(string userId, string newRole);

        /// <summary>
        /// Crear un nuevo usuario administrador
        /// </summary>
        Task<User?> CreateAdminUserAsync(CreateAdminRequest request);

        /// <summary>
        /// Activar/Desactivar usuario
        /// </summary>
        Task<bool> ToggleUserStatusAsync(string userId);

        /// <summary>
        /// Eliminar usuario (solo si no es el último admin)
        /// </summary>
        Task<bool> DeleteUserAsync(string userId);

        /// <summary>
        /// Obtener estadísticas de usuarios
        /// </summary>
        Task<UserStats> GetUserStatsAsync();

        /// <summary>
        /// Actualizar perfil de usuario
        /// </summary>
        Task<User?> UpdateProfileAsync(string userId, UpdateProfileRequest request);

        /// <summary>
        /// Resetear contraseña de un usuario (solo admins)
        /// </summary>
        Task<bool> ResetPasswordAsync(string userId, string newPassword);
    }

    /// <summary>
    /// Modelo para crear un usuario administrador
    /// </summary>
    public class CreateAdminRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
    }

    /// <summary>
    /// Estadísticas de usuarios del sistema
    /// </summary>
    public class UserStats
    {
        public int TotalUsers { get; set; }
        public int AdminUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public DateTime? LastUserCreated { get; set; }
    }

    /// <summary>
    /// Modelo para actualizar perfil de usuario
    /// </summary>
    public class UpdateProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
    }
}

