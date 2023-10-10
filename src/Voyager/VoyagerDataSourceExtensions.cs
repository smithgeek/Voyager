using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Voyager
{
	public static class VoyagerDataSourceExtensions
	{
		public static void MapVoyager(this IEndpointRouteBuilder endpoints, VoyagerMapOptions? options = null)
		{
			var voyagerRouteRegistrations = endpoints.ServiceProvider.GetRequiredService<List<VoyagerRouteRegistration>>();
			var voyagerOptionsHolder = endpoints.ServiceProvider.GetRequiredService<VoyagerOptionsHolder>();
			options ??= new VoyagerMapOptions();
			voyagerOptionsHolder.MapOptions = options;
			endpoints.DataSources.Add(new VoyagerDataSource(options, voyagerRouteRegistrations));
		}
	}
}