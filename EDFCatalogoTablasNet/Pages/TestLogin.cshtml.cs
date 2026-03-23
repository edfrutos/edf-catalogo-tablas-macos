using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EDFCatalogoTablasNet.Services;

namespace EDFCatalogoTablasNet.Pages
{
    [IgnoreAntiforgeryToken]
    public class TestLoginModel : PageModel
    {
        private readonly IAuthService _authService;

        public TestLoginModel(IAuthService authService)
        {
            _authService = authService;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Login directo con credenciales de admin para testing
                var user = await _authService.LoginAsync("admin@edf.com", "admin123");

                if (user != null)
                {
                    Console.WriteLine($"✅ Test Login exitoso: {user.Name} ({user.Email})");
                    return Redirect("/");
                }
                else
                {
                    Console.WriteLine("❌ Test Login falló");
                    TempData["ErrorMessage"] = "Error en el login de prueba";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en test login: {ex.Message}");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return Page();
            }
        }
    }
}
