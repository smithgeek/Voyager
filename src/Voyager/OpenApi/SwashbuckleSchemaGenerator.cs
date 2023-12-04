using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Text.Json;

namespace Voyager.OpenApi;

internal class SwashbuckleSchemaGenerator : IOpenApiSchemaGenerator
{
	private readonly ISchemaGenerator generator;
	private readonly SchemaRepository schemaRepository;

	public SwashbuckleSchemaGenerator(IServiceProvider serviceProvider)
	{
		generator = GetSchemaGenerator(serviceProvider);
		schemaRepository = serviceProvider.GetService<SchemaRepository>() ?? new();
	}

	public OpenApiSchema Generate(Type type)
	{
		var generatedType = generator.GenerateSchema(type, schemaRepository);
		if (generatedType.Reference != null)
		{
			return schemaRepository.Schemas[type.Name];
		}
		if(generatedType.Items?.Reference != null)
		{
			generatedType.Items = schemaRepository.Schemas[generatedType.Items.Reference.Id];
		}
		return generatedType;
	}

	private static ISchemaGenerator GetSchemaGenerator(IServiceProvider serviceProvider)
	{
		return serviceProvider.GetService<ISchemaGenerator>()
			?? new SchemaGenerator(
				serviceProvider.GetService<SchemaGeneratorOptions>() ?? new(),
				serviceProvider.GetService<ISerializerDataContractResolver>() ??
					new JsonSerializerDataContractResolver(
						serviceProvider.GetService<IOptions<JsonOptions>>()?.Value?.JsonSerializerOptions
						?? new JsonSerializerOptions()));
	}
}