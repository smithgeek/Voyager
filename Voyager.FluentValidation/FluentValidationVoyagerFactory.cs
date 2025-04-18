namespace Voyager.Validation.FluentValidation;

using global::FluentValidation;
using Microsoft.Extensions.DependencyInjection;

internal class FluentValidationVoyagerFactory : IVoyagerValidationAdapterFactory
{
	public IVoyagerValidationAdapter<TRequest>? Create<TRequest>(IServiceProvider serviceProvider)
	{
		var validator = serviceProvider.GetService<AbstractValidator<TRequest>>();
		if (validator != null)
		{
			return new FluentValidationAdapter<TRequest>(validator);
		}
		return null;
	}
}
