using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Voyager;
using Voyager.Azure.Functions;

[assembly: FunctionsStartup(typeof(SampleFunctionsApp.Startup))]

namespace SampleFunctionsApp
{
	public class Startup : FunctionsStartup
	{
		public Startup()
		{
		}

		public void Configure(IApplicationBuilder app)
		{
			Shared.Configure.ConfigureApp(app);
		}

		public override void Configure(IFunctionsHostBuilder builder)
		{
			var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
			var section = config.GetSection("Voyager");
			var cs = section.GetChildren().ToList();
			builder.AddVoyager(ConfigureServices, Configure);
		}

		public void ConfigureServices(IServiceCollection services)
		{
			Shared.Configure.ConfigureServices(services);
		}
	}
}