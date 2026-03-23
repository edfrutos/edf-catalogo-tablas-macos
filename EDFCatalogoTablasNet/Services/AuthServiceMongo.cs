using EDFCatalogoTablasNet.Models;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Servicio de autenticación usando MongoDB
    /// </summary>
    public class AuthServiceMongo : IAuthService
    {
        private readonly MongoDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuthStateService _authStateService;
        private readonly CircuitIdService _circuitIdService;
        private readonly ILogger<AuthServiceMongo> _logger;
        private const string SESSION_USER_KEY = "CurrentUser";

        public AuthServiceMongo(
            MongoDbContext context,
            IHttpContextAccessor httpContextAccessor,
            AuthStateService authStateService,
            CircuitIdService circuitIdService,
            ILogger<AuthServiceMongo> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _authStateService = authStateService;
            _circuitIdService = circuitIdService;
            _logger = logger;
        }

        public bool IsAuthenticated
        {
            get
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    var sessionId = session.Id;
                    var authState = _authStateService.GetUserAuth(sessionId);
                    return authState != null;
                }

                // Fallback para Blazor Server cuando HttpContext no está disponible
                // Intentar obtener desde las variables estáticas globales
                return !string.IsNullOrEmpty(GetGlobalUserEmail());
            }
        }

        public string? CurrentUserEmail
        {
            get
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    var sessionId = session.Id;
                    var authState = _authStateService.GetUserAuth(sessionId);
                    _logger.LogDebug("[CurrentUserEmail] Session available - SessionId: {SessionId}, AuthState: {AuthState}",
                        sessionId, authState != null ? authState.Email : "NULL");
                    return authState?.Email;
                }

                // Fallback 1: Usar CircuitId para Blazor Server
                var circuitId = _circuitIdService.CircuitId;
                if (!string.IsNullOrEmpty(circuitId))
                {
                    var circuitAuthState = _authStateService.GetUserAuth($"circuit_{circuitId}");
                    if (circuitAuthState != null)
                    {
                        _logger.LogDebug("[CurrentUserEmail] Using circuit fallback - CircuitId: {CircuitId}, Email: {Email}",
                            circuitId, circuitAuthState.Email);
                        return circuitAuthState.Email;
                    }
                }

                // Fallback 2: Variables globales estáticas
                var globalEmail = GetGlobalUserEmail();
                _logger.LogDebug("[CurrentUserEmail] Using global fallback: {Email}", globalEmail ?? "NULL");
                return globalEmail;
            }
        }

        public string? CurrentUserRole
        {
            get
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    var sessionId = session.Id;
                    var authState = _authStateService.GetUserAuth(sessionId);
                    return authState?.Role;
                }

                // Fallback 1: Usar CircuitId para Blazor Server
                var circuitId = _circuitIdService.CircuitId;
                if (!string.IsNullOrEmpty(circuitId))
                {
                    var circuitAuthState = _authStateService.GetUserAuth($"circuit_{circuitId}");
                    if (circuitAuthState != null)
                    {
                        _logger.LogDebug("[CurrentUserRole] Using circuit fallback - CircuitId: {CircuitId}, Role: {Role}",
                            circuitId, circuitAuthState.Role);
                        return circuitAuthState.Role;
                    }
                }

                // Fallback 2: Variables globales estáticas
                return GetGlobalUserRole();
            }
        }

        public event EventHandler<bool>? AuthenticationStateChanged;

        // Variables estáticas globales para fallback en Blazor Server
        private static string? _globalUserEmail;
        private static string? _globalUserRole;
        private static readonly object _globalLock = new object();

        private string? GetGlobalUserEmail()
        {
            lock (_globalLock)
            {
                return _globalUserEmail;
            }
        }

        private string? GetGlobalUserRole()
        {
            lock (_globalLock)
            {
                return _globalUserRole;
            }
        }

        private void SetGlobalUser(string? email, string? role)
        {
            lock (_globalLock)
            {
                _globalUserEmail = email;
                _globalUserRole = role;
            }
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _context.Users
                    .Find(u => u.Email.ToLower() == email.ToLower() && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user != null && VerifyPassword(password, user.Password))
                {
                    // Actualizar última vez que inició sesión
                    user.LastLoginAt = DateTime.Now;
                    await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

                    // Guardar en sesión
                    var session = _httpContextAccessor.HttpContext?.Session;
                    if (session != null)
                    {
                        session.SetString(SESSION_USER_KEY, user.Email);
                        session.SetString("UserRole", user.Role);

                        // Guardar en AuthStateService
                        _authStateService.SetUserAuth(session.Id, user.Email, user.Role);
                        _logger.LogInformation("[LoginAsync] Session saved - SessionId: {SessionId}, Email: {Email}, Role: {Role}",
                            session.Id, user.Email, user.Role);
                    }
                    else
                    {
                        _logger.LogWarning("[LoginAsync] Session is NULL - cannot save session data");
                    }

                    // También guardar en variables globales para Blazor Server
                    SetGlobalUser(user.Email, user.Role);
                    _logger.LogInformation("[LoginAsync] Global variables set - Email: {Email}, Role: {Role}", user.Email, user.Role);

                    AuthenticationStateChanged?.Invoke(this, true);
                    _logger.LogInformation("Usuario {Email} inició sesión correctamente", email);

                    return user;
                }

                _logger.LogWarning("Intento de login fallido para {Email}", email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LoginAsync para {Email}", email);
                return null;
            }
        }

        public async Task<bool> RegisterAsync(RegisterModel model)
        {
            try
            {
                // Verificar si el email ya existe
                if (await EmailExistsAsync(model.Email))
                {
                    _logger.LogWarning("Intento de registro con email existente: {Email}", model.Email);
                    return false;
                }

                var newUser = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = HashPassword(model.Password),
                    Phone = model.Phone,
                    Company = model.Company,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    Role = "User"
                };

                await _context.Users.InsertOneAsync(newUser);
                _logger.LogInformation("Nuevo usuario registrado: {Email}", model.Email);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RegisterAsync para {Email}", model.Email);
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            await Task.CompletedTask;
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.Remove(SESSION_USER_KEY);

                // Limpiar AuthStateService
                _authStateService.ClearUserAuth(session.Id);
            }

            // También limpiar variables globales
            SetGlobalUser(null, null);

            AuthenticationStateChanged?.Invoke(this, false);
            _logger.LogInformation("Usuario cerró sesión");
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var email = CurrentUserEmail;
            if (string.IsNullOrEmpty(email))
                return null;

            return await _context.Users
                .Find(u => u.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var count = await _context.Users
                .CountDocumentsAsync(u => u.Email.ToLower() == email.ToLower());
            return count > 0;
        }

        public async Task<bool> SendContactMessageAsync(ContactModel model)
        {
            try
            {
                await _context.Contacts.InsertOneAsync(model);

                _logger.LogInformation("📧 Mensaje de contacto recibido:");
                _logger.LogInformation("   De: {Name} ({Email})", model.Name, model.Email);
                _logger.LogInformation("   Asunto: {Subject}", model.Subject);
                _logger.LogInformation("   Tipo: {QueryType}", model.QueryType);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar mensaje de contacto");
                return false;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Find(u => u.IsActive)
                .ToListAsync();
        }

        public async Task<List<ContactModel>> GetContactMessagesAsync()
        {
            return await _context.Contacts
                .Find(_ => true)
                .SortByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        #region Métodos auxiliares para hash de contraseñas

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "EDF_SALT_2024"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private static bool VerifyPassword(string password, string hashedPassword)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hashedPassword);
        }

        #endregion
    }
}

