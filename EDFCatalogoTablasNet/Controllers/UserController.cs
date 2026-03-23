using Microsoft.AspNetCore.Mvc;
using EDFCatalogoTablasNet.Services;
using EDFCatalogoTablasNet.Models;

namespace EDFCatalogoTablasNet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IAuthService authService, ILogger<UserController> logger)
        {
            _userService = userService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los usuarios (Solo Admin)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            try
            {
                // Verificar que el usuario actual es admin
                if (!_authService.IsAuthenticated || _authService.CurrentUserRole != "Admin")
                {
                    return Unauthorized(new { message = "No tienes permisos para acceder a esta información" });
                }

                var users = await _userService.GetAllUsersAsync();

                // Ocultar contraseñas en la respuesta
                foreach (var user in users)
                {
                    user.Password = "***HIDDEN***";
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de usuarios");
                return StatusCode(500, new { message = "Error al obtener usuarios" });
            }
        }

        /// <summary>
        /// Obtener usuario por ID (Solo Admin)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUserById(string id)
        {
            try
            {
                if (!_authService.IsAuthenticated || _authService.CurrentUserRole != "Admin")
                {
                    return Unauthorized(new { message = "No tienes permisos para acceder a esta información" });
                }

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                user.Password = "***HIDDEN***";
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario {UserId}", id);
                return StatusCode(500, new { message = "Error al obtener usuario" });
            }
        }

        /// <summary>
        /// Actualizar rol de usuario (Solo Admin)
        /// </summary>
        [HttpPut("{id}/role")]
        public async Task<ActionResult> UpdateUserRole(string id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                if (!_authService.IsAuthenticated || _authService.CurrentUserRole != "Admin")
                {
                    return Unauthorized(new { message = "No tienes permisos para realizar esta acción" });
                }

                var success = await _userService.UpdateUserRoleAsync(id, request.NewRole);
                if (!success)
                {
                    return BadRequest(new { message = "No se pudo actualizar el rol. Verifica que no sea el último administrador." });
                }

                return Ok(new { message = "Rol actualizado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar rol del usuario {UserId}", id);
                return StatusCode(500, new { message = "Error al actualizar rol" });
            }
        }

        /// <summary>
        /// Crear nuevo usuario administrador (Solo Admin)
        /// </summary>
        [HttpPost("admin")]
        public async Task<ActionResult<User>> CreateAdminUser([FromBody] CreateAdminRequest request)
        {
            try
            {
                if (!_authService.IsAuthenticated || _authService.CurrentUserRole != "Admin")
                {
                    return Unauthorized(new { message = "No tienes permisos para realizar esta acción" });
                }

                var newAdmin = await _userService.CreateAdminUserAsync(request);
                if (newAdmin == null)
                {
                    return BadRequest(new { message = "No se pudo crear el usuario. El email podría estar duplicado." });
                }

                newAdmin.Password = "***HIDDEN***";
                return Ok(newAdmin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario administrador");
                return StatusCode(500, new { message = "Error al crear usuario administrador" });
            }
        }

        /// <summary>
        /// Activar/Desactivar usuario (Solo Admin)
        /// </summary>
        [HttpPut("{id}/toggle-status")]
        public async Task<ActionResult> ToggleUserStatus(string id)
        {
            try
            {
                if (!_authService.IsAuthenticated || _authService.CurrentUserRole != "Admin")
                {
                    return Unauthorized(new { message = "No tienes permisos para realizar esta acción" });
                }

                var success = await _userService.ToggleUserStatusAsync(id);
                if (!success)
                {
                    return BadRequest(new { message = "No se pudo cambiar el estado. Verifica que no sea el último administrador activo." });
                }

                return Ok(new { message = "Estado actualizado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del usuario {UserId}", id);
                return StatusCode(500, new { message = "Error al cambiar estado" });
            }
        }

        /// <summary>
        /// Eliminar usuario (Solo Admin)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            try
            {
                if (!_authService.IsAuthenticated || _authService.CurrentUserRole != "Admin")
                {
                    return Unauthorized(new { message = "No tienes permisos para realizar esta acción" });
                }

                var success = await _userService.DeleteUserAsync(id);
                if (!success)
                {
                    return BadRequest(new { message = "No se pudo eliminar el usuario. Verifica que no sea el último administrador." });
                }

                return Ok(new { message = "Usuario eliminado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UserId}", id);
                return StatusCode(500, new { message = "Error al eliminar usuario" });
            }
        }

        /// <summary>
        /// Obtener estadísticas de usuarios (Solo Admin)
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<UserStats>> GetUserStats()
        {
            try
            {
                if (!_authService.IsAuthenticated || _authService.CurrentUserRole != "Admin")
                {
                    return Unauthorized(new { message = "No tienes permisos para acceder a esta información" });
                }

                var stats = await _userService.GetUserStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de usuarios");
                return StatusCode(500, new { message = "Error al obtener estadísticas" });
            }
        }

        /// <summary>
        /// Actualizar perfil del usuario actual
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult<User>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                if (!_authService.IsAuthenticated)
                {
                    return Unauthorized(new { message = "Debes iniciar sesión para actualizar tu perfil" });
                }

                // Obtener el usuario actual
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null || string.IsNullOrEmpty(currentUser.Id))
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                var updatedUser = await _userService.UpdateProfileAsync(currentUser.Id, request);
                if (updatedUser == null)
                {
                    return BadRequest(new { message = "No se pudo actualizar el perfil" });
                }

                // Ocultar contraseña
                updatedUser.Password = "***HIDDEN***";
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil");
                return StatusCode(500, new { message = "Error al actualizar perfil" });
            }
        }

        /// <summary>
        /// Resetear contraseña de un usuario (solo admins)
        /// </summary>
        [HttpPost("{userId}/reset-password")]
        public async Task<ActionResult> ResetPassword(string userId, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!_authService.IsAuthenticated || _authService.CurrentUserRole != "Admin")
                {
                    return Unauthorized(new { message = "No tienes permisos para realizar esta acción" });
                }

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new { message = "La contraseña no puede estar vacía" });
                }

                var success = await _userService.ResetPasswordAsync(userId, request.NewPassword);
                if (success)
                {
                    return Ok(new { message = "Contraseña reseteada correctamente" });
                }

                return BadRequest(new { message = "No se pudo resetear la contraseña" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear contraseña");
                return StatusCode(500, new { message = "Error al resetear contraseña" });
            }
        }
    }

    /// <summary>
    /// Modelo para actualizar rol de usuario
    /// </summary>
    public class UpdateRoleRequest
    {
        public string NewRole { get; set; } = string.Empty;
    }

    /// <summary>
    /// Modelo para resetear contraseña
    /// </summary>
    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}

