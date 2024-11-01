using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;

namespace Voyager.OpenApi;

internal class VoyagerOpenApiDocumentFilter : IDocumentFilter
{
	internal static ISchemaIdGenerator? schemaIdGenerator;
	private readonly IServiceProvider serviceProvider;

	public VoyagerOpenApiDocumentFilter(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
	}

	public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
	{
		if (schemaIdGenerator != null)
		{
			var mappings = serviceProvider.GetService<IEnumerable<IVoyagerMapping>>();
			if (mappings is not null)
			{
				foreach (var mapping in mappings)
				{
					foreach (var component in mapping.GetOpenApiComponents(schemaIdGenerator))
					{
						swaggerDoc.Components.Schemas.Add(schemaIdGenerator.GetId(component.Key), component.Value);
					}
				}
			}
		}
	}
}
