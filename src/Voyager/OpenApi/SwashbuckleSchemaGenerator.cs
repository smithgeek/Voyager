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

public static class SchemaIdMapping
{
	public static Dictionary<Type, string> Mappings { get; } = new();

	public static void Add<Type>(string name)
	{
		Mappings.Add(typeof(Type), name);
	}
}

internal class SwashbuckleSchemaGenerator : IOpenApiSchemaGenerator
{
	private readonly ISchemaGenerator generator;
	private readonly SchemaRepository schemaRepository;

	public SwashbuckleSchemaGenerator(IServiceProvider? serviceProvider)
	{
		generator = GetSchemaGenerator(serviceProvider);
		schemaRepository = serviceProvider?.GetService<SchemaRepository>() ?? new();
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

	private static string SchemaIdSelector(Type type, Func<string> getId)
	{
		if (SchemaIdMapping.Mappings.TryGetValue(type, out var mappedId))
		{
			return mappedId;
		}
		var count = 2;
		var originalId = getId();
		var id = originalId;
		while (SchemaIdMapping.Mappings.ContainsValue(id))
		{
			id = $"{originalId}{count++}";
		}
		SchemaIdMapping.Mappings[type] = id;
		return id;
	}

	private ISchemaGenerator GetSchemaGenerator(IServiceProvider? serviceProvider)
	{
		var generatorOptions = serviceProvider?.GetService<SchemaGeneratorOptions>() ?? new();
		var originalSelector = generatorOptions.SchemaIdSelector;
		generatorOptions.SchemaIdSelector = type => SchemaIdSelector(type, () => originalSelector(type));
		return serviceProvider?.GetService<ISchemaGenerator>()
			?? new SchemaGenerator(
				generatorOptions,
				serviceProvider?.GetService<ISerializerDataContractResolver>() ??
					new JsonSerializerDataContractResolver(
						serviceProvider?.GetService<IOptions<JsonOptions>>()?.Value?.JsonSerializerOptions
						?? new JsonSerializerOptions()));
	}
}