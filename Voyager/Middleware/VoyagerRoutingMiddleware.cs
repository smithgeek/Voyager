using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voyager.Api;

namespace Voyager.Middleware
{
	public class VoyagerRoutingMiddleware
	{
		private readonly ModelBinder modelBinder;
		private readonly RequestDelegate next;
		private readonly IEnumerable<EndpointRoute> routes;

		public VoyagerRoutingMiddleware(RequestDelegate next, IEnumerable<EndpointRoute> routes, ModelBinder modelBinder)
		{
			this.next = next;
			this.routes = routes;
			this.modelBinder = modelBinder;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			foreach (var route in routes)
			{
				if (context.Request.Method == route.Method)
				{
					if (route.TemplateMatcher.TryMatch(context.Request.Path.Value, context.Request.RouteValues))
					{
						context.SetEndpoint(new Endpoint(c => Route(route.RequestType, c), new EndpointMetadataCollection(Enumerable.Empty<object>()), route.RequestType.Name));
						break;
					}
				}
			}
			await next(context);
		}

		private async Task Route(Type requestType, HttpContext context)
		{
			var mediatorRequest = await modelBinder.Bind(context, requestType);
			var mediator = context.RequestServices.GetService<IMediator>();
			var response = await mediator.Send(mediatorRequest);
			await context.WriteResultAsync(response);
		}
	}
}