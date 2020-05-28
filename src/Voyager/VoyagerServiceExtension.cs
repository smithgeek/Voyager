using Microsoft.Extensions.DependencyInjection;
using System;
using Voyager.Configuration;

namespace Voyager
{
	public static class VoyagerServiceExtension
	{
		public static void AddVoyager(this IServiceCollection services, Action<VoyagerConfigurationBuilder> configure = null)
		{
			var configurationBuilder = new VoyagerConfigurationBuilder();
			configure?.Invoke(configurationBuilder);
			VoyagerStartup.Configure(configurationBuilder, services);
		}
	}
}