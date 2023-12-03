using Microsoft.OpenApi.Models;
using System;

namespace Voyager.OpenApi;

public interface IOpenApiSchemaGenerator
{
	OpenApiSchema Generate(Type type);
}
