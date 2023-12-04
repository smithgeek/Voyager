using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace Voyager.OpenApi;

internal class VoyagerOpenApiDocumentFilter : IDocumentFilter
{
	private readonly IServiceProvider serviceProvider;

	public VoyagerOpenApiDocumentFilter(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
	}

	public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
	{
		var refTypes = OpenApiSchemaGenerator.GetSchemaGenerator(serviceProvider).GetSchemas();
		foreach (var type in refTypes)
		{
			swaggerDoc.Components.Schemas.Add(type);
		}
	}
}