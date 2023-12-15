using Microsoft.Extensions.DependencyInjection;
using System;

namespace Voyager.OpenApi;

internal static class OpenApiSchemaGenerator
{
	private static IOpenApiSchemaGenerator? generator;

	public static IOpenApiSchemaGenerator GetSchemaGenerator(IServiceProvider? services = null)
	{
		generator ??= services?.GetService<IOpenApiSchemaGenerator>() ?? new SwashbuckleSchemaGenerator(services);
		return generator;
	}
}