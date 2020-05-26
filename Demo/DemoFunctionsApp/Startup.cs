using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Voyager;
using Voyager.Azure.Functions;

[assembly: FunctionsStartup(typeof(DemoFunctionsApp.Startup))]

namespace DemoFunctionsApp
{
	public class Startup : FunctionsStartup
	{
		public Startup()
		{
		}

		public void Configure(IApplicationBuilder app)
		{
			app.UsePathBase("/api");
			app.UseVoyagerExceptionHandler();
			app.UseVoyagerRouting();
			app.UseMiddleware<SampleMiddleware>();
			app.UseVoyagerEndpoints();
		}

		public override void Configure(IFunctionsHostBuilder builder)
		{
			builder.AddVoyager(ConfigureServices, Configure);
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddVoyager(c =>
			{
				c.AddAssemblyWith<Startup>();
				c.AddAssemblyWith<SampleMiddleware>();
			});
		}
	}
}