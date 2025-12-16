using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomerLy.Presentation.Pages.User.Payments
{
    [Authorize(Roles = "User")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
