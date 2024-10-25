using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Voyager.OpenApi;

internal class SwaggerRequiredSchemaFilter : ISchemaFilter
{
	public void Apply(OpenApiSchema schema, SchemaFilterContext context)
	{
		if (schema.Properties == null)
		{
			return;
		}

		foreach (var schemProp in schema.Properties)
		{
			if (schemProp.Value.Nullable)
			{
				continue;
			}

			if (!schema.Required.Contains(schemProp.Key))
			{
				schema.Required.Add(schemProp.Key);
			}
		}
	}
}