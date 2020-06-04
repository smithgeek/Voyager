using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Shared;
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
			app.UsePathBase("/api");
			app.UseVoyagerExceptionHandler();
			app.UseRouting();
			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "API V1");
			});
			app.UseMiddleware<SampleMiddleware>();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapVoyager();
				endpoints.MapControllers();
			});
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
			services.AddVoyager(c =>
			{
				c.AddAssemblyWith<Startup>();
				c.AddAssemblyWith<SampleMiddleware>();
			});

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
			});
		}
	}
}