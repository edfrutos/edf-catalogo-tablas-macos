using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EDFCatalogoTablasNet.Services;

namespace EDFCatalogoTablasNet.Pages
{
    [IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;

        public LoginModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet(string? registered)
        {
            // Si ya está autenticado, redirigir al home
            if (_authService.IsAuthenticated)
            {
                return Redirect("/");
            }

            // Verificar si viene desde registro
            if (!string.IsNullOrEmpty(registered) && registered.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                SuccessMessage = "¡Cuenta creada exitosamente! Ahora puedes iniciar sesión con tus credenciales.";
            }

            // Verificar si hay mensajes en TempData
            ErrorMessage ??= TempData["ErrorMessage"] as string;
            SuccessMessage ??= TempData["SuccessMessage"] as string;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) && Request.Form.ContainsKey("Email"))
                Email = Request.Form["Email"].ToString();

            if (string.IsNullOrEmpty(Password) && Request.Form.ContainsKey("Password"))
                Password = Request.Form["Password"].ToString();

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Por favor, completa el correo o usuario y la contraseña.";
                return Page();
            }

            try
            {
                var user = await _authService.LoginAsync(Email, Password);

                if (user != null)
                {
                    SuccessMessage = $"¡Bienvenido de vuelta, {user.Name}!";
                    return Redirect("/");
                }
                else
                {
                    ErrorMessage = "Credenciales incorrectas. Intenta de nuevo.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error durante el login: {ex.Message}";
                return Page();
            }
        }
    }
}
