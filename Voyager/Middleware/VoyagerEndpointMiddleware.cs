using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Threading.Tasks;

namespace Voyager.Middleware
{
	public class VoyagerEndpointMiddleware
	{
		private readonly RequestDelegate next;

		public VoyagerEndpointMiddleware(RequestDelegate next)
		{
			this.next = next;
		}

		public async Task Invoke(HttpContext context)
		{
			var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
			if (endpoint?.RequestDelegate != null)
			{
				await endpoint.RequestDelegate(context);
				return;
			}

			await next(context);
		}
	}
}