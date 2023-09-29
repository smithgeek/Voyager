using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using Voyager.Configuration;

namespace Voyager
{
	public static class VoyagerServiceExtension
	{
		public static void AddVoyager(this IServiceCollection services, Action<VoyagerConfigurationBuilder>? configure = null)
		{
			var configurationBuilder = new VoyagerConfigurationBuilder();
			if (configure == null)
			{
				configurationBuilder.Assemblies.Add(Assembly.GetCallingAssembly());
			}
			else
			{
				configure?.Invoke(configurationBuilder);
			}
			VoyagerStartup.Configure(configurationBuilder, services);
		}
	}
}