using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;

namespace Voyager.OpenApi;

public interface IOperationBuilder
{
	void AddBody(Type bodyType);

	void AddParameter(string name, ParameterLocation location, Type type, bool required);

	void AddResponse(int statusCode, Type? type);

	OpenApiOperation Build();
}

public class OperationBuilder : IOperationBuilder
{
	private readonly OpenApiOperation operation;
	private readonly Dictionary<int, List<Type>> responses = new();
	private readonly IOpenApiSchemaGenerator schemaGenerator;

	public OperationBuilder(IOpenApiSchemaGenerator schemaGenerator, OpenApiOperation operation)
	{
		this.schemaGenerator = schemaGenerator;
		this.operation = operation;
	}

	public void AddBody(Type bodyType)
	{
		operation.RequestBody = new OpenApiRequestBody
		{
			Content = new Dictionary<string, OpenApiMediaType>
				{
					{
						"application/json",
						new OpenApiMediaType
						{
							Schema = schemaGenerator.Generate(bodyType)
						}
					}
				}
		};
	}

	public void AddParameter(string name, ParameterLocation location, Type type, bool required)
	{
		operation.Parameters.Add(new OpenApiParameter
		{
			Name = name,
			In = location,
			Schema = schemaGenerator.Generate(type),
			Required = required
		});
	}

	public void AddResponse(int statusCode, Type? type)
	{
		if (!responses.ContainsKey(statusCode))
		{
			responses[statusCode] = new();
		}
		if (type != null)
		{
			responses[statusCode].Add(type);
		}
	}

	public OpenApiOperation Build()
	{
		operation.Responses = new OpenApiResponses();
		foreach (var response in responses)
		{
			operation.Responses.Add(response.Key.ToString(), new OpenApiResponse
			{
				Content = new Dictionary<string, OpenApiMediaType>
					{
						{
							"application/json",
							new OpenApiMediaType
							{
								Schema = schemaGenerator.Generate(response.Value.ToArray())
							}
						}
					}
			});
		}
		return operation;
	}
}