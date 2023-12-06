using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using Voyager.OpenApi;

namespace Voyager;

internal class VoyagerSwaggerOperationFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<VoyagerOpenApiMetadata>().FirstOrDefault(); 
		if(metadata is not null)
		{
			operation.Responses = metadata.Operation.Responses;
			operation.Parameters = metadata.Operation.Parameters;
			operation.RequestBody = metadata.Operation.RequestBody;
		}
	}
}
