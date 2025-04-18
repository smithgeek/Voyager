namespace Voyager.Validation.Validot;

using global::Validot;
using Microsoft.Extensions.DependencyInjection;

internal class ValidotValidationVoyagerFactory : IVoyagerValidationAdapterFactory
{
	public IVoyagerValidationAdapter<TRequest>? Create<TRequest>(IServiceProvider serviceProvider)
	{
		var validator = serviceProvider.GetService<IValidator<TRequest>>();
		if (validator != null)
		{
			return new ValidotValidationAdapter<TRequest>(validator);
		}
		return null;
	}
}
