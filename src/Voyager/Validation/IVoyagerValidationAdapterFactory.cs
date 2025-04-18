using System;

namespace Voyager.Validation;

public interface IVoyagerValidationAdapterFactory
{
	public IVoyagerValidationAdapter<TRequest>? Create<TRequest>(IServiceProvider serviceProvider);
}