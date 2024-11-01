using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Voyager;
using Voyager.OpenApi;

namespace Microsoft.AspNetCore.Builder;

public static class MapVoyagerExtension
{
	public static WebApplication MapVoyager(this WebApplication app)
	{
		var mappings = app.Services.GetService<IEnumerable<IVoyagerMapping>>();
		if (mappings is not null)
		{
			var schemaIdGenerator = app.Services.GetService<ISchemaIdGenerator>() ?? new SchemaIdGenerator();
			VoyagerOpenApiDocumentFilter.schemaIdGenerator = schemaIdGenerator;
			var componentTypes = mappings.SelectMany(m => m.GetComponentTypes()).Distinct();
			schemaIdGenerator.AddTypes(componentTypes);
			foreach (var mapping in mappings)
			{
				mapping.MapEndpoints(app, schemaIdGenerator);
			}
		}
		return app;
	}
}
