using Microsoft.OpenApi.Models;
using System;

namespace Voyager.OpenApi;

public class VoyagerOpenApiMetadata
{
	public Func<OpenApiOperation, OpenApiOperation> UpdateOperationData { get; init; }

	public VoyagerOpenApiMetadata(Func<OpenApiOperation, OpenApiOperation> updateOperationData)
	{
		UpdateOperationData = updateOperationData;
	}
}
