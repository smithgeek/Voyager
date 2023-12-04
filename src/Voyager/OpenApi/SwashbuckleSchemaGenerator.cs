using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
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

	public OpenApiSchema? Generate(IEnumerable<Type> types)
	{
		if (!types.Any())
		{
			return null;
		}
		var schemas = types.Select(Generate);
		if (schemas.Count() == 1)
		{
			return schemas.First();
		}
		return new OpenApiSchema
		{
			OneOf = schemas.ToList()
		};
	}

	public OpenApiSchema Generate(Type type)
	{
		return generator.GenerateSchema(type, schemaRepository);
	}

	public IEnumerable<KeyValuePair<string, OpenApiSchema>> GetSchemas()
	{
		return schemaRepository.Schemas;
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