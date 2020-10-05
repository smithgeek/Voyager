using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Voyager.Api;
using Voyager.Api.Authorization;

namespace Voyager
{
	public class VoyagerDataSource : EndpointDataSource
	{
		private readonly VoyagerMapOptions mapOptions;
		private readonly IEnumerable<VoyagerRouteDefinition> voyagerRoutes;

		public VoyagerDataSource(IEnumerable<VoyagerRouteDefinition> voyagerRoutes, VoyagerMapOptions mapOptions)
		{
			this.voyagerRoutes = voyagerRoutes;
			this.mapOptions = mapOptions;
		}

		public override IReadOnlyList<Endpoint> Endpoints
		{
			get
			{
				var endpoints = new List<Endpoint>();
				foreach (var voyagerRoute in voyagerRoutes)
				{
					var builder = new RouteEndpointBuilder(c => Route(voyagerRoute.RequestType, c), RoutePatternFactory.Parse($"{mapOptions.Prefix}{voyagerRoute.Template}"), 0);
					builder.Metadata.Add(new HttpMethodMetadata(new[] { voyagerRoute.Method }));
					foreach (var attribute in voyagerRoute.RequestType.GetCustomAttributes(false))
					{
						builder.Metadata.Add(attribute);
					}
					var policies = voyagerRoute.RequestType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Enforce<>));
					foreach (var policy in policies.Select(p => p.GetGenericArguments()[0].FullName))
					{
						builder.Metadata.Add(new AuthorizeAttribute(policy));
					}
					var endpoint = builder.Build();
					endpoints.Add(endpoint);
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