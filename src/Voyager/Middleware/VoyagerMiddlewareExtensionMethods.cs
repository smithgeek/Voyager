using Microsoft.AspNetCore.Builder;
using Voyager.Middleware;

namespace Voyager
{
	public static class VoyagerMiddlewareExtensionMethods
	{
		public static IApplicationBuilder UseVoyagerExceptionHandler(this IApplicationBuilder applicationBuilder)
		{
			applicationBuilder.UseMiddleware<VoyagerExceptionHandlerMiddleware>();
			return applicationBuilder;
		}
	}
}