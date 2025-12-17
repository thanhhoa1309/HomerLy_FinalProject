using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomerLy.Presentation.Pages.Payment
{
    [Authorize]
    public class CancelModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? InvoiceId { get; set; }

        public void OnGet()
        {
        }
    }
}
