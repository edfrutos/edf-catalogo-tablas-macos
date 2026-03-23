using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
            // Debug: Verificar valores recibidos
            Console.WriteLine($"Email recibido: '{Email}'");
            Console.WriteLine($"Password recibido: '{Password}'");
            Console.WriteLine($"ModelState válido: {ModelState.IsValid}");
            Console.WriteLine($"Request Form Keys: {string.Join(", ", Request.Form.Keys)}");

            // Intentar obtener valores directamente del formulario si están vacíos
            if (string.IsNullOrEmpty(Email) && Request.Form.ContainsKey("Email"))
            {
                Email = Request.Form["Email"].ToString();
                Console.WriteLine($"Email desde Form: '{Email}'");
            }

            if (string.IsNullOrEmpty(Password) && Request.Form.ContainsKey("Password"))
            {
                Password = Request.Form["Password"].ToString();
                Console.WriteLine($"Password desde Form: '{Password}'");
            }

            // Validación simple manual
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Por favor, completa el email y la contraseña.";
                Console.WriteLine($"Validación fallida - Email: '{Email}', Password: '{Password}'");
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
