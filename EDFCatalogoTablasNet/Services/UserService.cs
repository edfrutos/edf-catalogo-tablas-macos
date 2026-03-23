using EDFCatalogoTablasNet.Models;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Servicio de gestión de usuarios con MongoDB
    /// </summary>
    public class UserService : IUserService
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(MongoDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Find(_ => true)
                .SortByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _context.Users
                .Find(u => u.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Find(u => u.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateUserRoleAsync(string userId, string newRole)
        {
            try
            {
                // Validar que el rol es válido
                if (newRole != "Admin" && newRole != "User" && newRole != "Guest")
                {
                    _logger.LogWarning("Intento de asignar rol inválido: {Role}", newRole);
                    return false;
                }

                // Si se intenta degradar de Admin, verificar que no sea el último admin
                var user = await GetUserByIdAsync(userId);
                if (user != null && user.Role == "Admin" && newRole != "Admin")
                {
                    var adminCount = await _context.Users.CountDocumentsAsync(u => u.Role == "Admin");
                    if (adminCount <= 1)
                    {
                        _logger.LogWarning("No se puede degradar al último administrador del sistema");
                        return false;
                    }
                }

                var update = Builders<User>.Update.Set(u => u.Role, newRole);
                var result = await _context.Users.UpdateOneAsync(u => u.Id == userId, update);

                if (result.IsAcknowledged && result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Rol actualizado para usuario {UserId} a {Role}", userId, newRole);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar rol del usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<User?> CreateAdminUserAsync(CreateAdminRequest request)
        {
            try
            {
                // Verificar si el email ya existe
                var existingUser = await GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Intento de crear admin con email duplicado: {Email}", request.Email);
                    return null;
                }

                var newAdmin = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = HashPassword(request.Password),
                    Phone = request.Phone,
                    Company = request.Company,
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.InsertOneAsync(newAdmin);
                _logger.LogInformation("Nuevo usuario administrador creado: {Email}", request.Email);

                return newAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario administrador");
                return null;
            }
        }

        public async Task<bool> ToggleUserStatusAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                // Si es admin y está activo, verificar que no sea el último admin activo
                if (user.Role == "Admin" && user.IsActive)
                {
                    var activeAdminCount = await _context.Users.CountDocumentsAsync(
                        u => u.Role == "Admin" && u.IsActive == true
                    );
                    if (activeAdminCount <= 1)
                    {
                        _logger.LogWarning("No se puede desactivar al último administrador activo");
                        return false;
                    }
                }

                var newStatus = !user.IsActive;
                var update = Builders<User>.Update.Set(u => u.IsActive, newStatus);
                var result = await _context.Users.UpdateOneAsync(u => u.Id == userId, update);

                if (result.IsAcknowledged && result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Estado del usuario {UserId} cambiado a {Status}", userId, newStatus ? "Activo" : "Inactivo");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                // No permitir eliminar al último administrador
                if (user.Role == "Admin")
                {
                    var adminCount = await _context.Users.CountDocumentsAsync(u => u.Role == "Admin");
                    if (adminCount <= 1)
                    {
                        _logger.LogWarning("No se puede eliminar al último administrador del sistema");
                        return false;
                    }
                }

                var result = await _context.Users.DeleteOneAsync(u => u.Id == userId);

                if (result.IsAcknowledged && result.DeletedCount > 0)
                {
                    _logger.LogInformation("Usuario {UserId} eliminado correctamente", userId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<UserStats> GetUserStatsAsync()
        {
            try
            {
                var allUsers = await GetAllUsersAsync();

                return new UserStats
                {
                    TotalUsers = allUsers.Count,
                    AdminUsers = allUsers.Count(u => u.Role == "Admin"),
                    ActiveUsers = allUsers.Count(u => u.IsActive),
                    InactiveUsers = allUsers.Count(u => !u.IsActive),
                    LastUserCreated = allUsers.OrderByDescending(u => u.CreatedAt).FirstOrDefault()?.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de usuarios");
                return new UserStats();
            }
        }

        public async Task<User?> UpdateProfileAsync(string userId, UpdateProfileRequest request)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Usuario {UserId} no encontrado para actualizar perfil", userId);
                    return null;
                }

                // Actualizar campos del perfil
                user.Name = request.Name;
                user.Phone = request.Phone;
                user.Company = request.Company;
                user.ProfileImageUrl = request.ProfileImageUrl;
                user.Address = request.Address;
                user.Occupation = request.Occupation;

                var result = await _context.Users.ReplaceOneAsync(u => u.Id == userId, user);

                if (result.IsAcknowledged && result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Perfil actualizado para usuario {UserId}", userId);
                    return user;
                }

                return user; // Retornar user incluso si no hubo cambios
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil del usuario {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Usuario {UserId} no encontrado para resetear contraseña", userId);
                    return false;
                }

                user.Password = HashPassword(newPassword);

                var result = await _context.Users.ReplaceOneAsync(u => u.Id == userId, user);

                if (result.IsAcknowledged && result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Contraseña reseteada para usuario {UserId}", userId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear contraseña del usuario {UserId}", userId);
                return false;
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "EDF_SALT_2024"));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}

