using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RazoreApp.Pages
{
	public class IndexModel : PageModel
	{
		private readonly IConfiguration configuration;

		public IndexModel(IConfiguration configuration)
		{
			this.configuration = configuration;
		}

		public string Message { get; set; }

		[BindProperty, DataType(DataType.Password)]
		public string Password { get; set; }

		[BindProperty]
		public string UserName { get; set; }

		public async Task<IActionResult> OnPost()
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.Name, UserName)
			};
			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
			if (HttpContext.Request.Query.ContainsKey("ReturnUrl"))
			{
				return Redirect(HttpContext.Request.Query["ReturnUrl"].ToString());
			}
			return RedirectToPage("/admin/index");
		}
	}
}