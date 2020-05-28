using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using Voyager.Api;

namespace Voyager.Azure.Functions
{
	public static class IFunctionsHostBuilderExtensions
	{
		public static IFunctionsHostBuilder AddVoyager(this IFunctionsHostBuilder builder, Action<IServiceCollection> configureServices, Action<IApplicationBuilder> configure)
		{
			builder.Services.AddSingleton(provider =>
			{
				var applicationBuilder = new ApplicationBuilder(provider);
				configure(applicationBuilder);
				return new HttpRouter(applicationBuilder);
			});
			configureServices(builder.Services);
			return builder;
		}

		internal static IFunctionsHostBuilder AddAppSettingsToConfiguration(this IFunctionsHostBuilder builder)
		{
			var currentDirectory = "/home/site/wwwroot";
			var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
			if (isLocal)
			{
				currentDirectory = Environment.CurrentDirectory;
			}

			var tmpConfig = new ConfigurationBuilder()
				.SetBasePath(currentDirectory)
				.AddJsonFile("appsettings.json")
				.Build();

			var environmentName = tmpConfig["Environment"];

			var configurationBuilder = new ConfigurationBuilder();

			var descriptor = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
			if (descriptor?.ImplementationInstance is IConfiguration configRoot)
			{
				configurationBuilder.AddConfiguration(configRoot);
			}

			var configuration = configurationBuilder.SetBasePath(currentDirectory)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
				.Build();

			builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), configuration));

			return builder;
		}
	}
}