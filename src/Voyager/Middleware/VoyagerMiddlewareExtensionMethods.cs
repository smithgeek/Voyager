using Microsoft.AspNetCore.Builder;
using Voyager.Middleware;

namespace Voyager
{
	public static class VoyagerMiddlewareExtensionMethods
	{
		public static IApplicationBuilder UseVoyagerEndpoints(this IApplicationBuilder applicationBuilder)
		{
			applicationBuilder.UseMiddleware<VoyagerEndpointMiddleware>();
			return applicationBuilder;
		}

		public static IApplicationBuilder UseVoyagerExceptionHandler(this IApplicationBuilder applicationBuilder)
		{
			applicationBuilder.UseMiddleware<VoyagerExceptionHandlerMiddleware>();
			return applicationBuilder;
		}

		public static IApplicationBuilder UseVoyagerRouting(this IApplicationBuilder applicationBuilder)
		{
			applicationBuilder.UseMiddleware<VoyagerRoutingMiddleware>();
			return applicationBuilder;
		}
	}
}