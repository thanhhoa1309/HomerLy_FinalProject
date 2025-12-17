using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomerLy.Presentation.Pages.Payment
{
    [Authorize]
    public class SuccessModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? SessionId { get; set; }

        public void OnGet()
        {
        }
    }
}
