#nullable enable
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Voyager.OpenApi;

public class OpenApiTransformerRepo
{
	private Dictionary<Type, List<string>> requestTypes = [];

	public void AddRequestBody<Type>(List<string> excludeProperties)
	{
		if (!requestTypes.ContainsKey(typeof(Type)))
		{
			requestTypes.Add(typeof(Type), excludeProperties);
		}
	}

	public bool TryGetExcludedProperties(Type type, [NotNullWhen(true)] out List<string>? excludedProperties)
	{
		return requestTypes.TryGetValue(type, out excludedProperties);
	}
}

public static class VoyagerOpenApiExtensions
{
	public static void AddVoyager(this OpenApiOptions openApiOptions)
	{
		openApiOptions.AddSchemaTransformer(static (schema, context, token) =>
		{
			if (context.JsonTypeInfo.Type.IsAssignableTo(typeof(IOneOf)))
			{
				schema.OneOf = [.. schema.Properties.Values];
				schema.Properties.Clear();
			}
			else
			{
				var repo = context.ApplicationServices.GetService<OpenApiTransformerRepo>();
				if (repo?.TryGetExcludedProperties(context.JsonTypeInfo.Type, out var excludedProperties) ?? false)
				{
					foreach (var prop in excludedProperties)
					{
						var key = schema.Properties.Keys.FirstOrDefault(key => key.Equals(prop, StringComparison.OrdinalIgnoreCase));
						if (key != null)
						{
							schema.Properties.Remove(key);
						}
					}
				}
			}
			return Task.CompletedTask;
		});
	}

	public static bool IsNullableReferenceType(PropertyInfo p)
	{
		var writeState = new NullabilityInfoContext().Create(p).WriteState;
		return writeState is not NullabilityState.NotNull;
	}
}