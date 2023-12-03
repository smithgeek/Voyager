using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;

namespace Voyager.OpenApi;

public static class OpenApiSchemaGenerator
{
	private static IOpenApiSchemaGenerator? generator;

	public static OpenApiSchema GenerateSchema(IServiceProvider services, Type type)
	{
		return (generator ?? GetSchemaGenerator(services)).Generate(type);
	}

	private static IOpenApiSchemaGenerator GetSchemaGenerator(IServiceProvider services)
	{
		var gen = services.GetService<IOpenApiSchemaGenerator>() ?? new SwashbuckleSchemaGenerator(services);
		generator = gen;
		return gen;
	}
}