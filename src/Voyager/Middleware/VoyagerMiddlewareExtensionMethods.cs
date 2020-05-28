using Voyager.Middleware;

namespace Microsoft.AspNetCore.Builder
{
	public static class VoyagerMiddlewareExtensionMethods
	{
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

		public static IApplicationBuilder UseVoyagerEndpoints(this IApplicationBuilder applicationBuilder)
		{
			applicationBuilder.UseMiddleware<VoyagerEndpointMiddleware>();
			return applicationBuilder;
		}
	}
}