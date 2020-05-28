using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Shared
{
	public class SampleMiddleware
	{
		private readonly RequestDelegate next;

		public SampleMiddleware(RequestDelegate next)
		{
			this.next = next;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			context.Response.Headers.Add("voyager", "supported");
			await next(context);
		}
	}
}