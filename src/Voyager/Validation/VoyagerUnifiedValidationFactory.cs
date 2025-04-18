using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voyager.Validation;
public class VoyagerUnifiedValidationFactory(IEnumerable<IVoyagerValidationAdapterFactory> factories,
	IServiceScopeFactory serviceScopeFactory) : IDisposable
{
	private readonly IServiceScope serviceScope = serviceScopeFactory.CreateScope();

	public IVoyagerValidationAdapter<TRequest> Create<TRequest>(bool required = false)
	{
		foreach (var factory in factories)
		{
			var adapter = factory.Create<TRequest>(serviceScope.ServiceProvider);
			if (adapter != null)
			{
				return adapter;
			}
		}
		if (required)
		{
			return new DefaultValidationAdapter<TRequest>();
		}
		return new NullAdapter<TRequest>();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		serviceScope.Dispose();
	}
}

internal class NullAdapter<TRequest> : IVoyagerValidationAdapter<TRequest>
{
	public Task<ValidationSummary> Validate(TRequest request, Func<string, string> getPropertyName)
	{
		return Task.FromResult(new ValidationSummary { IsValid = true });
	}
}
