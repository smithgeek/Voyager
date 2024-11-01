using Microsoft.OpenApi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Voyager.OpenApi;

public interface IOperationBuilder
{
	void AddBody(Type bodyType, string[]? excludedProperties = null);
	void AddBody(OpenApiSchema schema);
	void AddParameter(string name, ParameterLocation location, Type type, bool required);

	void AddResponse(int statusCode, Type? type, string[]? excludeProperties = null);
	void AddResponse(int statusCode, OpenApiSchema schema);

	OpenApiOperation Build();
}

public class OperationBuilder : IOperationBuilder
{
	private readonly OpenApiOperation operation;
	private readonly Dictionary<int, List<(Type Type, string[]? ExcludedProperties)>> responses = new();
	private readonly IOpenApiSchemaGenerator schemaGenerator;

	public OperationBuilder(IOpenApiSchemaGenerator schemaGenerator, OpenApiOperation operation)
	{
		this.schemaGenerator = schemaGenerator;
		this.operation = operation;
	}

	public void AddBody(Type bodyType, string[]? excludedProperties)
	{
		operation.RequestBody = new OpenApiRequestBody
		{
			Content = new Dictionary<string, OpenApiMediaType>
				{
					{
						"application/json",
						new OpenApiMediaType
						{
							Schema = GenerateSchema(bodyType, excludedProperties)
						}
					}
				}
		};
	}

	public void AddBody(OpenApiSchema schema)
	{

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

	public void AddResponse(int statusCode, Type? type, string[]? excludeProperties)
	{
		if (!responses.ContainsKey(statusCode))
		{
			responses[statusCode] = new();
		}
		if (type != null)
		{
			responses[statusCode].Add((type, excludeProperties));
		}
	}

	public void AddResponse(int statusCode, OpenApiSchema schema)
	{

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
								Schema = response.Key == 200
									? GenerateSchema(response.Value)
									: schemaGenerator.Generate(response.Value.Select(t => t.Type).ToArray())
							}
						}
					}
			});
		}
		return operation;
	}

	private OpenApiSchema GenerateSchema(List<(Type Type, string[]? ExcludedProperties)> responseTypes)
	{
		if (responseTypes.Count == 1)
		{
			return GenerateSchema(responseTypes[0].Type, responseTypes[0].ExcludedProperties);
		}
		else
		{
			return new()
			{
				OneOf = responseTypes.Select(tuple => GenerateSchema(tuple.Type, tuple.ExcludedProperties)).ToArray()
			};
		}
	}

	private OpenApiSchema GenerateSchema(Type Type, string[]? ExcludedProperties = null, bool allowReferences = false)
	{
		//var schema = schemaGenerator.Generate(Type);
		//if (schema.Type == "object")
		//{
		//	var properties = Type.GetProperties();
		//	foreach (var prop in properties)
		//	{
		//		if (ExcludedProperties != null && ExcludedProperties.Contains(prop.Name))
		//		{
		//			schema.Properties.Remove(prop.Name);
		//		}
		//		else if (NullableChecker.IsNullableType(prop, out var _))
		//		{
		//			schema.Properties[prop.Name].Nullable = true;
		//		}
		//		else
		//		{
		//			schema.Required.Add(prop.Name);
		//		}
		//	}
		//}
		//return schema;
		var fullName = Type.FullName;
		var generatedSchema = schemaGenerator.Generate(Type);
		if (generatedSchema.Reference == null || allowReferences)
		{
			return generatedSchema;
		}

		IEnumerable<PropertyInfo> properties = Type.GetProperties();
		if (ExcludedProperties != null)
		{
			properties = properties.Where(p => !ExcludedProperties.Contains(p.Name));
		}
		var required = new HashSet<string>();
		var schema = new OpenApiSchema
		{
			AdditionalPropertiesAllowed = false,
			Properties = properties.ToDictionary(p => p.Name, p =>
			{
				var nullable = NullableChecker.IsNullableType(p, out var propertyType);
				if (!nullable)
				{
					required.Add(p.Name);
				}
				var childSchema = GenerateSchema(propertyType, allowReferences: true);
				childSchema.Nullable = nullable;
				if (childSchema.Type == "array")
				{
					var itemType = TypeHelper.GetIEnumerableItemType(propertyType);
					if (itemType != null && NullableChecker.IsNullableValueType(itemType, out var underlyingItemType))
					{
						childSchema.Items.Nullable = true;
					}
				}
				return childSchema;
			}),
			Required = required
		};
		return schema;
	}

	public OpenApiOperation BuildOld()
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
								Schema = schemaGenerator.Generate(response.Value.Select(t => t.Type).ToArray())
							}
						}
					}
			});
		}
		return operation;
	}
}

public class NullableChecker
{
	public static bool IsNullableReferenceType(PropertyInfo p)
	{
		var writeState = new NullabilityInfoContext().Create(p).WriteState;
		return writeState is not NullabilityState.NotNull;
	}

	public static bool IsNullableValueType(Type type, out Type underlyingType)
	{
		// Check if it's a value type and a Nullable<T>
		if (type.IsValueType)
		{
			underlyingType = Nullable.GetUnderlyingType(type) ?? type;
			return underlyingType != null;
		}
		underlyingType = type;
		return false;
	}

	public static bool IsNullableType(PropertyInfo property, out Type underlyingType)
	{
		// Check if it's a value type and a Nullable<T>
		if (property.PropertyType.IsValueType)
		{
			underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
			return underlyingType != null;
		}

		// Check if it's a nullable reference type
		underlyingType = property.PropertyType;
		return IsNullableReferenceType(property);
	}
}

public class TypeHelper
{
	public static Type? GetIEnumerableItemType(Type type)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
		{
			var itemType = type.GetGenericArguments()[0];
			return itemType;
		}
		// Check if the type implements IEnumerable<T>
		var ienumerableType = type.GetInterfaces()
								  .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

		if (ienumerableType != null)
		{
			// Return the type argument of IEnumerable<T>
			return ienumerableType.GetGenericArguments()[0];
		}

		// If it's an array, return the element type
		if (type.IsArray)
		{
			return type.GetElementType();
		}

		// Non-generic IEnumerable fallback (e.g., ArrayList)
		if (typeof(IEnumerable).IsAssignableFrom(type))
		{
			return typeof(object);
		}

		// If type does not implement IEnumerable or is not an array
		return null;
	}
}
