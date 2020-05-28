using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;
using System.Linq;
using Voyager;

namespace SampleApi
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			app.UseVoyagerExceptionHandler();
			app.UseHttpsRedirection();

			app.UseVoyagerRouting();
			app.UseRouting();

			app.UseAuthorization();
			app.UseMiddleware<SampleMiddleware>();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
			app.UseVoyagerEndpoints();
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			var section = Configuration.GetSection("Voyager");
			var cs = section.GetChildren().ToList();
			services.AddControllers();
			services.AddVoyager(c =>
			{
				c.AddAssemblyWith<Startup>();
				c.AddAssemblyWith<SampleMiddleware>();
			});
		}
	}
}