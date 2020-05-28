using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Voyager.Api;
using Voyager.Middleware;
using Xunit;

namespace Voyager.UnitTests.Middleware
{
	public class VoyagerRoutingMiddlewareTests
	{
		[Fact]
		public async Task FeatureIsSet()
		{
			var services = new ServiceCollection();
			services.AddVoyager(c => c.AddAssemblyWith<VoyagerRoutingMiddlewareTests>());
			var provider = services.BuildServiceProvider();
			var route = new RouteAttribute(HttpMethod.Get, "/test");
			var endpoints = new[] { route.ToEndpointRoute(typeof(int)) };
			var middleware = new VoyagerRoutingMiddleware(context => Task.CompletedTask, endpoints, provider.GetService<ModelBinder>());
			var context = TestFactory.HttpContext();
			context.Request.Method = "GET";
			context.Request.Path = "/test";
			await middleware.InvokeAsync(context);
			var feature = context.Features.Get<VoyagerEndpointFeature>();
			feature.Should().NotBeNull();
			feature.Endpoint.RequestDelegate.Should().NotBeNull();
			feature.Endpoint.Name.Should().Be("Int32");
		}
	}
}