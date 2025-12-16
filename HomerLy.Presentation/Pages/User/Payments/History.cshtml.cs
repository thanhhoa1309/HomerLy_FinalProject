using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomerLy.Presentation.Pages.User.Payments
{
    [Authorize(Roles = "User")]
    public class HistoryModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
