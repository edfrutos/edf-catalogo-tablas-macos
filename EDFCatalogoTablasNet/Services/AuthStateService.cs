using System.Collections.Concurrent;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Servicio singleton para compartir estado de autenticación entre Razor Pages y Blazor Server
    /// </summary>
    public class AuthStateService
    {
        private readonly ConcurrentDictionary<string, AuthState> _authStates = new();
        private readonly object _lock = new object();

        public void SetUserAuth(string sessionId, string email, string role)
        {
            lock (_lock)
            {
                _authStates[sessionId] = new AuthState
                {
                    Email = email,
                    Role = role,
                    LastAccess = DateTime.UtcNow
                };
            }
        }

        public AuthState? GetUserAuth(string sessionId)
        {
            lock (_lock)
            {
                if (_authStates.TryGetValue(sessionId, out var authState))
                {
                    // Actualizar último acceso
                    authState.LastAccess = DateTime.UtcNow;
                    return authState;
                }
                return null;
            }
        }

        public void ClearUserAuth(string sessionId)
        {
            lock (_lock)
            {
                _authStates.TryRemove(sessionId, out _);
            }
        }

        public void CleanupExpiredSessions()
        {
            lock (_lock)
            {
                var expiredKeys = _authStates
                    .Where(kvp => DateTime.UtcNow - kvp.Value.LastAccess > TimeSpan.FromHours(24))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _authStates.TryRemove(key, out _);
                }
            }
        }

        public class AuthState
        {
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public DateTime LastAccess { get; set; }
        }
    }
}
