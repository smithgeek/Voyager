using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Voyager;

namespace Shared
{
	public static class Configure
	{
		public static void ConfigureApp(IApplicationBuilder app)
		{
			app.UsePathBase("/api");
			app.UseVoyagerExceptionHandler();
			app.UseHttpsRedirection();

			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
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

		public static void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();
			services.AddVoyager(c =>
			{
				c.AddAssemblyWith<SampleMiddleware>();
			});

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
			});
		}
	}
}