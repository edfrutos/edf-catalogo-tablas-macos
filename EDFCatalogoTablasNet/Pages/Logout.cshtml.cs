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
            await _authService.LogoutAsync();
            TempData["SuccessMessage"] = "Has cerrado sesión correctamente.";
            return Redirect("/login");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // También manejar POST requests
            await _authService.LogoutAsync();
            return Redirect("/login");
        }
    }
}
