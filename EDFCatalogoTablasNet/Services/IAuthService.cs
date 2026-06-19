using EDFCatalogoTablasNet.Models;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Interfaz para el servicio de autenticación
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Autentica un usuario con email y contraseña
        /// </summary>
        Task<User?> LoginAsync(string email, string password);

        /// <summary>
        /// Registra un nuevo usuario
        /// </summary>
        Task<bool> RegisterAsync(RegisterModel model);

        /// <summary>
        /// Cierra la sesión del usuario actual
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Obtiene el usuario actualmente autenticado
        /// </summary>
        Task<User?> GetCurrentUserAsync();

        /// <summary>
        /// Verifica si un usuario está autenticado
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Obtiene el email del usuario actual
        /// </summary>
        string? CurrentUserEmail { get; }

        /// <summary>
        /// Obtiene el rol del usuario actual
        /// </summary>
        string? CurrentUserRole { get; }

        /// <summary>
        /// Verifica si un email ya existe en el sistema
        /// </summary>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>
        /// Envía un mensaje de contacto
        /// </summary>
        Task<bool> SendContactMessageAsync(ContactModel model);

        /// <summary>
        /// Evento que se dispara cuando el estado de autenticación cambia
        /// </summary>
        event EventHandler<bool>? AuthenticationStateChanged;

        /// <summary>
        /// Notifica a los suscriptores el estado actual (p. ej. tras rehidratar sesión en el circuito Blazor).
        /// </summary>
        void NotifyAuthenticationStateChanged();
    }
}
