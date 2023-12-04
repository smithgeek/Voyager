using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;

namespace Voyager.OpenApi;

public interface IOpenApiSchemaGenerator
{
	OpenApiSchema? Generate(IEnumerable<Type> types);

	OpenApiSchema Generate(Type type);

	IEnumerable<KeyValuePair<string, OpenApiSchema>> GetSchemas();
}