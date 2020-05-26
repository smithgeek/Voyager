using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Voyager.Configuration.Extensions
{
	public static class IServiceCollectionExtensions
	{
		public static void RegisterConfiguration<TConfiguration>(this IServiceCollection services, string sectionName = null, Action<TConfiguration> postInitCallback = null)
			where TConfiguration : class, new()
		{
			if (!services.Any(s => s.ServiceType == typeof(TConfiguration)))
			{
				sectionName = string.Join(":", new[] { "Voyager", sectionName }.Where(s => s != null));
				services.AddSingleton((serviceProvider) =>
				{
					var configuration = serviceProvider.GetService<IConfiguration>();
					var customConfig = new TConfiguration();
					if (configuration != null)
					{
						configuration.GetSection(sectionName).Bind(customConfig);
					}
					postInitCallback?.Invoke(customConfig);
					return customConfig;
				});
			}
		}
	}
}