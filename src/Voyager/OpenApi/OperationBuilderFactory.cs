using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;

namespace Voyager.OpenApi;

public class OperationBuilderFactory
{
	public static IOperationBuilder Create(IServiceProvider serviceProvider, OpenApiOperation openApiOperation)
	{
		var factoryFunc = serviceProvider.GetService<Func<OpenApiOperation, IOperationBuilder>>();
		if (factoryFunc == null)
		{
			return new OperationBuilder(OpenApiSchemaGenerator.GetSchemaGenerator(serviceProvider), openApiOperation);
		}
		return factoryFunc(openApiOperation);
	}
}