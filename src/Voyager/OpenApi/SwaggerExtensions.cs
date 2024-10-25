using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Voyager.OpenApi;

namespace Voyager;

public static class SwaggerExtensions
{
	public static void AddVoyager(this SwaggerGenOptions options)
	{
		options.DocumentFilter<VoyagerOpenApiDocumentFilter>();
		options.OperationFilter<VoyagerSwaggerOperationFilter>();
		options.SchemaFilter<SwaggerRequiredSchemaFilter>();
	}
}
