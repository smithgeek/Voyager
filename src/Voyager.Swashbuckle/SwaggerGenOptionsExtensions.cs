using Swashbuckle.AspNetCore.SwaggerGen;
using Voyager.Swashbuckle;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class SwaggerGenOptionsExtensions
	{
		public static void AddVoyager(this SwaggerGenOptions options)
		{
			options.OperationFilter<VoyagerOperationFilter>();
			options.DocumentFilter<VoyagerDocumentFilter>();
		}
	}
}