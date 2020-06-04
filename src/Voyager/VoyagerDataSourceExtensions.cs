using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voyager.Api;

namespace Voyager
{
	public static class VoyagerDataSourceExtensions
	{
		public static void MapVoyager(this IEndpointRouteBuilder endpoints)
		{
			var voyagerEndpoints = endpoints.ServiceProvider.GetRequiredService<IEnumerable<VoyagerRoute>>();
			endpoints.DataSources.Add(new VoyagerDataSource(voyagerEndpoints));
		}
	}

	public class VoyagerDataSource : EndpointDataSource
	{
		private readonly IEnumerable<VoyagerRoute> voyagerRoutes;

		public VoyagerDataSource(IEnumerable<VoyagerRoute> voyagerRoutes)
		{
			this.voyagerRoutes = voyagerRoutes;
		}

		public override IReadOnlyList<Endpoint> Endpoints
		{
			get
			{
				var endpoints = new List<Endpoint>();
				foreach (var voyagerRoute in voyagerRoutes)
				{
					var builder = new RouteEndpointBuilder(c => Route(voyagerRoute.RequestType, c), RoutePatternFactory.Parse(voyagerRoute.Template), 0);
					builder.Metadata.Add(new HttpMethodMetadata(new[] { voyagerRoute.Method }));
					endpoints.Add(builder.Build());
				}
				return endpoints;
			}
		}

		public override IChangeToken GetChangeToken()
		{
			return new CancellationChangeToken(CancellationToken.None);
		}

		private async Task Route(Type requestType, HttpContext context)
		{
			var modelBinder = context.RequestServices.GetRequiredService<ModelBinder>();
			var mediatorRequest = await modelBinder.Bind(context, requestType);
			var mediator = context.RequestServices.GetService<IMediator>();
			var response = await mediator.Send(mediatorRequest);
			await context.WriteResultAsync(response);
		}
	}
}