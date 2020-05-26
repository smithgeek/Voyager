using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Voyager.Api
{
	public class HttpRouter
	{
		private readonly RequestDelegate next;

		public HttpRouter(IApplicationBuilder applicationBuilder)
		{
			next = applicationBuilder.Build();
		}

		public async Task<IActionResult> Route(HttpContext context)
		{
			await next(context);
			return new EmptyResult();
		}
	}
}