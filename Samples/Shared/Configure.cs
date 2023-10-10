using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Voyager;
using Voyager.AssemblyFactories;

namespace Shared
{
	public static class Configure
	{
		public static void ConfigureApp(IApplicationBuilder app)
		{
			app.UseVoyagerExceptionHandler();
			app.UseHttpsRedirection();

			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseSwagger(c =>
			{
				c.RouteTemplate = "docs/api/{documentName}/swagger.json";
			});
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/docs/api/v1/swagger.json", "API V1");
				c.RoutePrefix = "docs/api";
			});
			app.UseMiddleware<SampleMiddleware>();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapVoyager(new VoyagerMapOptions { Prefix = "api" });
				endpoints.MapControllers();
			});
		}

		public static void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();
			services.AddVoyager();

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Voyager Api", Version = "v1" });
				c.AddVoyager();
			});
		}
	}
}