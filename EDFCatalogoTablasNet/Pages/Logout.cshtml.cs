using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EDFCatalogoTablasNet.Services;

namespace EDFCatalogoTablasNet.Pages
{
    [IgnoreAntiforgeryToken]
    public class LogoutModel : PageModel
    {
        private readonly IAuthService _authService;

        public LogoutModel(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Cerrar sesión
            await _authService.LogoutAsync();

            // Opcional: agregar mensaje de confirmación
            TempData["SuccessMessage"] = "Has cerrado sesión correctamente.";

            // Mostrar la página de logout por 2 segundos antes de redirigir
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // También manejar POST requests
            await _authService.LogoutAsync();
            return Redirect("/login");
        }
    }
}
