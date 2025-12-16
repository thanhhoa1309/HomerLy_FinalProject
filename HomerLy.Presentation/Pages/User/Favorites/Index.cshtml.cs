using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomerLy.Presentation.Pages.User.Favorites
{
    [Authorize(Roles = "User")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            // TODO: Implement Get Favorites
        }
    }
}
