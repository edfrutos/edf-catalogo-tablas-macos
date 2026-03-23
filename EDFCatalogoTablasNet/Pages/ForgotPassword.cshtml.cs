using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using EDFCatalogoTablasNet.Services;

namespace EDFCatalogoTablasNet.Pages
{
    [IgnoreAntiforgeryToken]
    public class ForgotPasswordModel : PageModel
    {
        private readonly IAuthService _authService;

        public ForgotPasswordModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Ingresa un email válido")]
        public string Email { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool EmailSent { get; set; }

        public IActionResult OnGet()
        {
            // Si ya está autenticado, redirigir al home
            if (_authService.IsAuthenticated)
            {
                return Redirect("/");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email))
            {
                ErrorMessage = "Por favor, ingresa tu email.";
                return Page();
            }

            try
            {
                // Verificar si el email existe
                var emailExists = await _authService.EmailExistsAsync(Email);

                // Por seguridad, siempre mostramos el mismo mensaje
                // (no revelar si el email existe o no)
                EmailSent = true;
                SuccessMessage = "Si el email existe en nuestro sistema, recibirás instrucciones de recuperación.";

                if (emailExists)
                {
                    // En una implementación real, aquí se enviaría un email
                    // con un token de recuperación único
                    Console.WriteLine($"🔑 Solicitud de recuperación de contraseña para: {Email}");
                    Console.WriteLine($"   Token de recuperación: {Guid.NewGuid()}");
                    Console.WriteLine($"   Fecha: {DateTime.Now}");
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al procesar la solicitud: {ex.Message}";
                return Page();
            }
        }
    }
}

