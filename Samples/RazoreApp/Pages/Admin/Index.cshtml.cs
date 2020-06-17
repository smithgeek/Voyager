using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace RazoreApp.Pages.Admin
{
	public class IndexModel : PageModel
	{
		public void OnGet()
		{
		}

		public async Task<IActionResult> OnPost()
		{
			await HttpContext.SignOutAsync();
			return RedirectToPage("/index");
		}
	}
}