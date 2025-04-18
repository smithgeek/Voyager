namespace Voyager;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Voyager.Validation;
using Voyager.Validation.FluentValidation;

public static class DepdendencyInjectionExtensions
{
	public static void AddFluentValidation(this VoyagerConfig config, VoyagerFluentValidationOptions? options = null)
	{
		config.Services.AddTransient<IVoyagerValidationAdapterFactory, FluentValidationVoyagerFactory>();
		options ??= new();
		if (options.AutoRegister)
		{
			config.OnRegisterAssemblies((services, assemblies) =>
			{
				services.AddValidatorsFromAssemblies(assemblies);
			});
		}
	}
}
