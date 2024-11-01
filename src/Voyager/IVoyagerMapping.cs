using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using Voyager.OpenApi;

namespace Voyager;

public interface IVoyagerMapping
{
	void MapEndpoints(WebApplication app, ISchemaIdGenerator schemaIdGenerator);

	IDictionary<Type, OpenApiSchema> GetOpenApiComponents(ISchemaIdGenerator schemaIdGenerator);

	IEnumerable<Type> GetComponentTypes();
}
