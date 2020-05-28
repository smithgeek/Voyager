using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Voyager.Middleware;
using Xunit;

namespace Voyager.UnitTests.Middleware
{
	public class VoyagerEndpointMiddlewareTests
	{
		[Fact]
		public async Task DelegateIsRun()
		{
			var counter = 0;
			Task next(HttpContext context)
			{
				counter++;
				return Task.CompletedTask;
			}
			var endpointCounter = 0;
			var middleware = new VoyagerEndpointMiddleware(next);
			var context = TestFactory.HttpContext();
			context.Features.Set<VoyagerEndpointFeature>(new Voyager.Middleware.VoyagerRoutingMiddleware.EndpointFeature
			{
				Endpoint = new VoyagerEndpoint
				{
					RequestDelegate = (context) =>
					{
						endpointCounter++;
						return Task.CompletedTask;
					},
					Name = "Test"
				}
			});
			await middleware.Invoke(context);
			counter.Should().Be(0);
			endpointCounter.Should().Be(1);
		}
	}
}