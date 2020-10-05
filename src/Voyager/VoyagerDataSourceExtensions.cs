using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Voyager.Api;

namespace Voyager
{
	public static class VoyagerDataSourceExtensions
	{
		public static void MapVoyager(this IEndpointRouteBuilder endpoints, VoyagerMapOptions options = null)
		{
			var voyagerEndpoints = endpoints.ServiceProvider.GetRequiredService<IEnumerable<VoyagerRouteDefinition>>();
			var voyagerOptionsHolder = endpoints.ServiceProvider.GetRequiredService<VoyagerOptionsHolder>();
			options ??= new VoyagerMapOptions();
			voyagerOptionsHolder.MapOptions = options;
			endpoints.DataSources.Add(new VoyagerDataSource(voyagerEndpoints, options));
		}
	}
}