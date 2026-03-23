using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EDFCatalogoTablasNet.Services;

namespace EDFCatalogoTablasNet.Pages
{
    [IgnoreAntiforgeryToken]
    public class SimpleLoginModel : PageModel
    {
        private readonly IAuthService _authService;

        public SimpleLoginModel(IAuthService authService)
        {
            _authService = authService;
        }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            // Si ya está autenticado, redirigir al home
            if (_authService.IsAuthenticated)
            {
                return Redirect("/");
            }

            // Verificar si hay mensajes en TempData
            ErrorMessage = TempData["ErrorMessage"] as string;
            SuccessMessage = TempData["SuccessMessage"] as string;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Obtener datos directamente del Request.Form
                var email = Request.Form["email"].ToString();
                var password = Request.Form["password"].ToString();

                Console.WriteLine($"🔍 SimpleLogin - Email: '{email}'");
                Console.WriteLine($"🔍 SimpleLogin - Password: '{password}'");
                Console.WriteLine($"🔍 Form Keys: [{string.Join(", ", Request.Form.Keys)}]");

                // Validación simple
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ErrorMessage = "Por favor, completa el email y la contraseña.";
                    Console.WriteLine("❌ SimpleLogin - Campos vacíos");
                    return Page();
                }

                // Intentar login
                var user = await _authService.LoginAsync(email, password);

                if (user != null)
                {
                    Console.WriteLine($"✅ SimpleLogin exitoso: {user.Name} ({user.Email})");
                    SuccessMessage = $"¡Bienvenido de vuelta, {user.Name}!";
                    return Redirect("/");
                }
                else
                {
                    ErrorMessage = "Credenciales incorrectas. Intenta de nuevo.";
                    Console.WriteLine("❌ SimpleLogin - Credenciales incorrectas");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error durante el login: {ex.Message}";
                Console.WriteLine($"❌ SimpleLogin Exception: {ex.Message}");
                return Page();
            }
        }
    }
}
