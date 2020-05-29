using FluentAssertions;
using FluentAssertions.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using Voyager.Api;
using Voyager.Api.Authorization;
using Xunit;

namespace Voyager.UnitTests.Api
{
	public class BaseEndpointTests
	{
		[Fact]
		public async Task AnonymousPolicyAllowsAnyone()
		{
			var endpoint = GetEndpoint<AnonymousEndpoint>();
			var response = await endpoint.Handle(new AnonymousRequest(), CancellationToken.None);
			response.Value.Should().Be(3);
		}

		[Fact]
		public async Task NoPolciyAllowsAnyone()
		{
			var endpoint = GetEndpoint<NoPolicyEndpoint>();
			var response = await endpoint.Handle(new NoPolicyRequest(), CancellationToken.None);
			response.Value.Should().Be(3);
		}

		[Fact]
		public async Task PolicyIsEnforced()
		{
			var endpoint = GetEndpoint<AuthenticatedEndpoint>();
			var response = await endpoint.Handle(new AuthenticatedRequest(), CancellationToken.None);
			response.Result.Should().BeUnauthorizedResult();
		}

		private TEndpoint GetEndpoint<TEndpoint>()
		{
			var services = new ServiceCollection();
			services.AddVoyager(c => c.AddAssemblyWith<BaseEndpointTests>());
			var provider = services.BuildServiceProvider();
			var endpoint = provider.GetService<TEndpoint>();
			provider.GetService<IHttpContextAccessor>().HttpContext = TestFactory.HttpContext();
			return endpoint;
		}

		public class AnonymousEndpoint : BaseEndpointHandler<AnonymousRequest, ActionResult<int>>, Enforce<AnonymousPolicy>, Enforce<BlankPolicy>
		{
			internal override ActionResult<int> GetUnathorizedResponse()
			{
				return new UnauthorizedResult();
			}

			internal override Task<ActionResult<int>> HandleRequestInternal(AnonymousRequest request, CancellationToken cancellation)
			{
				return Task.FromResult<ActionResult<int>>(3);
			}
		}

		public class AnonymousRequest : EndpointRequest<int>
		{
		}

		public class AuthenticatedEndpoint : BaseEndpointHandler<AuthenticatedRequest, ActionResult<int>>, Enforce<AuthenticatedPolicy>
		{
			internal override ActionResult<int> GetUnathorizedResponse()
			{
				return new UnauthorizedResult();
			}

			internal override Task<ActionResult<int>> HandleRequestInternal(AuthenticatedRequest request, CancellationToken cancellation)
			{
				return Task.FromResult<ActionResult<int>>(3);
			}
		}

		public class AuthenticatedRequest : EndpointRequest<int>
		{
		}

		public class NoPolicyEndpoint : BaseEndpointHandler<NoPolicyRequest, ActionResult<int>>
		{
			internal override ActionResult<int> GetUnathorizedResponse()
			{
				return new UnauthorizedResult();
			}

			internal override Task<ActionResult<int>> HandleRequestInternal(NoPolicyRequest request, CancellationToken cancellation)
			{
				return Task.FromResult<ActionResult<int>>(3);
			}
		}

		public class NoPolicyRequest : EndpointRequest<int>
		{
		}
	}
}