using EDFCatalogoTablasNet;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Circuit Handler para mantener la información de autenticación en el circuito de Blazor Server
    /// </summary>
    public class AuthCircuitHandler : CircuitHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuthStateService _authStateService;
        private readonly CircuitIdService _circuitIdService;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthCircuitHandler> _logger;

        public AuthCircuitHandler(
            IHttpContextAccessor httpContextAccessor,
            AuthStateService authStateService,
            CircuitIdService circuitIdService,
            IAuthService authService,
            ILogger<AuthCircuitHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _authStateService = authStateService;
            _circuitIdService = circuitIdService;
            _authService = authService;
            _logger = logger;
        }

        public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            // Guardar el ID del circuito en el servicio
            _circuitIdService.CircuitId = circuit.Id;

            // Cuando se establece una conexión de Blazor Server, copiar la información de sesión
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                var session = httpContext.Session;
                await session.LoadAsync(cancellationToken);
                var sessionId = session.Id;
                var authState = _authStateService.GetUserAuth(sessionId);

                if (authState == null)
                {
                    const string sessionUserKey = "CurrentUser";
                    var email = session.GetString(sessionUserKey);
                    if (!string.IsNullOrEmpty(email))
                    {
                        var role = RoleHelper.NormalizeRole(session.GetString("UserRole") ?? "User");
                        _authStateService.SetUserAuth(sessionId, email, role);
                        authState = _authStateService.GetUserAuth(sessionId);
                        _logger.LogInformation("[AuthCircuitHandler] Auth rehidratada desde Session para {SessionId}", sessionId);
                    }
                }

                if (authState != null)
                {
                    _logger.LogInformation("[AuthCircuitHandler] Circuit {CircuitId} connected - User: {Email}, Role: {Role}",
                        circuit.Id, authState.Email, authState.Role);

                    _authStateService.SetUserAuth($"circuit_{circuit.Id}", authState.Email, authState.Role);
                    _authService.NotifyAuthenticationStateChanged();
                }
                else
                {
                    _logger.LogWarning("[AuthCircuitHandler] Circuit {CircuitId} connected - No auth for session {SessionId}",
                        circuit.Id, sessionId);
                }
            }
            else
            {
                _logger.LogWarning("[AuthCircuitHandler] Circuit {CircuitId} connected - No HttpContext or Session available",
                    circuit.Id);
            }

            await base.OnConnectionUpAsync(circuit, cancellationToken);
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            // Limpiar información del circuito cuando se desconecta
            _authStateService.ClearUserAuth($"circuit_{circuit.Id}");
            _logger.LogInformation("[AuthCircuitHandler] Circuit {CircuitId} disconnected", circuit.Id);

            return base.OnConnectionDownAsync(circuit, cancellationToken);
        }
    }
}

