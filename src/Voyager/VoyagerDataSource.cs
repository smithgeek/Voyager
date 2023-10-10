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
		private readonly IEnumerable<VoyagerRouteRegistration> routeDefinitions;

		public VoyagerDataSource(VoyagerMapOptions mapOptions, List<VoyagerRouteRegistration> routeDefinitions)
		{
			this.mapOptions = mapOptions;
			this.routeDefinitions = routeDefinitions;
		}

		public override IReadOnlyList<Endpoint> Endpoints
		{
			get
			{
				var endpoints = new List<Endpoint>();
				foreach (var routeDefinition in routeDefinitions)
				{
					var route = routeDefinition.RouteDefinition;
					var builder = new RouteEndpointBuilder(c => RouteSourceGenerator(routeDefinition, c),
						RoutePatternFactory.Parse($"{mapOptions.Prefix.TrimEnd('/')}/{route.Template.TrimStart('/')}"), 0);
					builder.Metadata.Add(new HttpMethodMetadata(new[] { route.Method }));
					foreach (var attribute in route.RequestType.GetCustomAttributes(false))
					{
						builder.Metadata.Add(attribute);
					}
					var policies = route.RequestType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Enforce<>));
					foreach (var policy in policies.Select(p => p.GetGenericArguments()[0].FullName))
					{
						if (policy != null)
						{
							builder.Metadata.Add(new AuthorizeAttribute(policy));
						}
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

		private static async Task RouteSourceGenerator(VoyagerRouteRegistration registration, HttpContext context)
		{
			var sender = context.RequestServices.GetService<ISender>();
			if (sender != null)
			{
				var dataProvider = new DataProvider(context);
				var request = await registration.RequestFactory(dataProvider);
				if (request != null)
				{
					var response = await sender.Send(request);
					if (response != null)
					{
						await context.WriteResultAsync(response);
					}
				}
			}
		}
	}

	public class VoyagerRouteRegistration
	{
		public required Func<VoyagerApiDescription> DescriptionFactory { get; init; }
		public required Func<DataProvider, Task<object?>> RequestFactory { get; init; }
		public required VoyagerRouteDefinition RouteDefinition { get; init; }
	}
}