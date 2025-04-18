namespace Voyager;
using Microsoft.Extensions.DependencyInjection;
using Validot;
using Voyager.Validation;
using Voyager.Validation.Validot;

public static class DepdendencyInjectionExtensions
{
	public static void AddVoyagerValidotValidation(this IServiceCollection services)
	{
		services.AddTransient<IVoyagerValidationAdapterFactory, ValidotValidationVoyagerFactory>();
	}

	public static void AddValidot(this VoyagerConfig config, Options? options = null)
	{
		config.Services.AddTransient<IVoyagerValidationAdapterFactory, ValidotValidationVoyagerFactory>();
		options ??= new();
		if (options.AutoRegister)
		{
			config.OnRegisterAssemblies((services, assemblies) =>
			{
				foreach (var holder in Validator.Factory.FetchHolders(assemblies.ToArray()).GroupBy(h => h.SpecifiedType))
				{
					services.AddSingleton(holder.First().ValidatorType, holder.First().CreateValidator());
				}
			});
		}
	}
}
