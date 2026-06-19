using EDFCatalogoTablasNet.Models;
using EDFCatalogoTablasNet;
using MongoDB.Bson;
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

        public bool IsAuthenticated =>
            !string.IsNullOrEmpty(CurrentUserEmail);

        public string? CurrentUserEmail
        {
            get
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    var sessionId = session.Id;
                    var authState = _authStateService.GetUserAuth(sessionId);
                    if (authState != null)
                    {
                        _logger.LogDebug("[CurrentUserEmail] SessionId {SessionId} -> {Email}", sessionId, authState.Email);
                        return authState.Email;
                    }

                    var fromSession = session.GetString(SESSION_USER_KEY);
                    if (!string.IsNullOrEmpty(fromSession))
                        return fromSession;
                }

                var circuitId = _circuitIdService.CircuitId;
                if (!string.IsNullOrEmpty(circuitId))
                {
                    var circuitAuth = _authStateService.GetUserAuth($"circuit_{circuitId}");
                    if (circuitAuth != null)
                        return circuitAuth.Email;
                }

                // No usar estado estático por proceso: filtraría sesión ajena a Razor Pages (/login) y E2E.
                return null;
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
                    if (authState != null)
                        return RoleHelper.NormalizeRole(authState.Role);

                    if (!string.IsNullOrEmpty(session.GetString(SESSION_USER_KEY)))
                        return RoleHelper.NormalizeRole(session.GetString("UserRole") ?? "User");
                }

                var circuitId = _circuitIdService.CircuitId;
                if (!string.IsNullOrEmpty(circuitId))
                {
                    var circuitAuth = _authStateService.GetUserAuth($"circuit_{circuitId}");
                    if (circuitAuth != null)
                        return RoleHelper.NormalizeRole(circuitAuth.Role);
                }

                return null;
            }
        }

        public event EventHandler<bool>? AuthenticationStateChanged;

        public void NotifyAuthenticationStateChanged()
        {
            AuthenticationStateChanged?.Invoke(this, IsAuthenticated);
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            try
            {
                var key = email.Trim();
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(password))
                    return null;

                var collName = _context.Users.CollectionNamespace.CollectionName;
                var bsonUsers = MongoUserBsonHelper.UsersCollection(_context.Database, collName);
                var doc = await bsonUsers
                    .Find(MongoUserBsonHelper.LoginKeyFilter(key))
                    .FirstOrDefaultAsync();

                if (doc == null)
                {
                    _logger.LogWarning("Intento de login fallido para {Key} (sin documento)", key);
                    return null;
                }

                if (!MongoUserBsonHelper.GetIsActive(doc))
                {
                    _logger.LogWarning("Intento de login con usuario inactivo: {Key}", key);
                    return null;
                }

                var storedHash = MongoUserBsonHelper.GetStoredPasswordHash(doc);
                if (string.IsNullOrEmpty(storedHash) || !VerifyPassword(password, storedHash))
                {
                    _logger.LogWarning("Intento de login fallido para {Key} (contraseña incorrecta o hash ausente)", key);
                    return null;
                }

                var canonicalEmail = MongoUserBsonHelper.GetCanonicalEmail(doc);
                if (string.IsNullOrEmpty(canonicalEmail))
                {
                    if (key.Contains('@', StringComparison.Ordinal))
                        canonicalEmail = key;
                    else
                    {
                        _logger.LogError("Login: documento sin Email/email en MongoDB para usuario {Key}", key);
                        return null;
                    }
                }

                var role = RoleHelper.NormalizeRole(MongoUserBsonHelper.GetRole(doc));
                var id = doc["_id"].ToString();

                var now = DateTime.UtcNow;
                await bsonUsers.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]),
                    Builders<BsonDocument>.Update.Set("LastLoginAt", now).Set("lastLoginAt", now));

                var user = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
                if (user == null)
                {
                    user = new User
                    {
                        Id = id,
                        Email = canonicalEmail,
                        Role = role,
                        Name = GetBsonString(doc, "Name", "name") ?? canonicalEmail,
                        LastLoginAt = now,
                        IsActive = true
                    };
                }
                else
                {
                    if (string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(canonicalEmail))
                        user.Email = canonicalEmail;
                    user.LastLoginAt = now;
                }

                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    session.SetString(SESSION_USER_KEY, canonicalEmail);
                    session.SetString("UserRole", role);
                    _authStateService.SetUserAuth(session.Id, canonicalEmail, role);
                    _logger.LogInformation("[LoginAsync] Session saved - SessionId: {SessionId}, Email: {Email}, Role: {Role}",
                        session.Id, canonicalEmail, role);
                }
                else
                    _logger.LogWarning("[LoginAsync] Session is NULL - cannot save session data");

                AuthenticationStateChanged?.Invoke(this, true);
                _logger.LogInformation("Usuario {Email} inició sesión correctamente", canonicalEmail);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LoginAsync para {Email}", email);
                return null;
            }
        }

        private static string? GetBsonString(BsonDocument doc, string pascalKey, string camelKey)
        {
            if (doc.TryGetValue(pascalKey, out var v) && v.IsString) return v.AsString;
            if (doc.TryGetValue(camelKey, out v) && v.IsString) return v.AsString;
            return null;
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
                session.Remove("UserRole");

                // Limpiar AuthStateService
                _authStateService.ClearUserAuth(session.Id);
            }

            var cid = _circuitIdService.CircuitId;
            if (!string.IsNullOrEmpty(cid))
                _authStateService.ClearUserAuth($"circuit_{cid}");

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

