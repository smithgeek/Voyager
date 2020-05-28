using Microsoft.Extensions.DependencyInjection;
using System;
using Voyager.Configuration;

namespace Voyager
{
	public static class VoyagerServiceExtension
	{
		public static void AddVoyager(this IServiceCollection services, Action<VoyagerConfigurationBuilder> configure)
		{
			services.AddMvcCore();
			var configurationBuilder = new VoyagerConfigurationBuilder();
			configure(configurationBuilder);
			VoyagerStartup.Configure(configurationBuilder, services);
		}
	}
}