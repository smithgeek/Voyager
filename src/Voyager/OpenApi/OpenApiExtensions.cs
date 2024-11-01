using Microsoft.OpenApi.Models;
using System.Collections.Generic;

namespace Voyager.OpenApi;

public class ResponseBuilder
{
	public Dictionary<string, List<OpenApiSchema>> Responses { get; } = new();

	public void Add(int statusCode, OpenApiSchema schema)
	{
		Add((statusCode).ToString(), schema);
	}

	public void Add(string statusCode, OpenApiSchema schema)
	{
		if (!Responses.TryGetValue(statusCode, out var schemas))
		{
			schemas = new();
			Responses[statusCode] = schemas;
		}
		schemas.Add(schema);
	}

	public void AddHttpValidationProblems()
	{
		Add("400", new OpenApiSchema
		{
			Reference = new OpenApiReference { Id = "HttpValidationProblems", Type = ReferenceType.Schema }
		});
	}

	public OpenApiSchema GetSchema(string statusCode)
	{
		if (Responses.TryGetValue(statusCode, out var schemas))
		{
			if (schemas.Count > 1)
			{
				return new OpenApiSchema
				{
					OneOf = schemas
				};
			}
			else if (schemas.Count == 1)
			{
				return schemas[0];
			}
		}
		return new OpenApiSchema { Type = "object" };
	}
}

public static class OpenApiExtensions
{
	private const string ApplicationJson = "application/json";

	public static void AddJsonRequestBody(this OpenApiOperation operation, OpenApiSchema schema)
	{
		operation.RequestBody ??= new();
		operation.RequestBody.Content ??= new Dictionary<string, OpenApiMediaType>();
		operation.RequestBody.Content[ApplicationJson] = new OpenApiMediaType
		{
			Schema = schema
		};
	}

	public static void AddJsonResponses(this OpenApiOperation operation, ResponseBuilder responseBuilder)
	{
		operation.Responses = new OpenApiResponses();
		foreach (var kvp in responseBuilder.Responses)
		{
			operation.Responses.Add(kvp.Key, new OpenApiResponse
			{
				Content = new Dictionary<string, OpenApiMediaType>
				{
					{
						ApplicationJson,
						new OpenApiMediaType
						{
							Schema = responseBuilder.GetSchema(kvp.Key)
						}
					}
				}
			});
		}
	}

	public static void Clear(this OpenApiOperation operation)
	{
		operation.Parameters.Clear();
		operation.Responses.Clear();
		operation.RequestBody = null;
	}

	public static void AddParameter(this OpenApiOperation operation, string name, ParameterLocation location, bool required, OpenApiSchema schema)
	{
		operation.Parameters.Add(new OpenApiParameter
		{
			Name = name,
			In = location,
			Schema = schema,
			Required = required
		});
	}


}
