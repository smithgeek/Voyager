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

		private TEndpoint GetEndpoint<TEndpoint>() where TEndpoint : InjectEndpointProps
		{
			var services = new ServiceCollection();
			services.AddVoyager(c => c.AddAssemblyWith<BaseEndpointTests>());
			var provider = services.BuildServiceProvider();
			var endpoint = provider.GetService<TEndpoint>();
			provider.GetService<IHttpContextAccessor>().HttpContext = TestFactory.HttpContext();
			return endpoint;
		}

		public class AnonymousEndpoint : EndpointHandler<AnonymousRequest, int>, Enforce<AnonymousPolicy>, Enforce<BlankPolicy>
		{
			public override ActionResult<int> HandleRequest(AnonymousRequest request)
			{
				return 3;
			}
		}

		public class AnonymousRequest : EndpointRequest<int>
		{
		}

		public class AuthenticatedEndpoint : EndpointHandler<AuthenticatedRequest, int>, Enforce<AuthenticatedPolicy>
		{
			public override ActionResult<int> HandleRequest(AuthenticatedRequest request)
			{
				return 3;
			}
		}

		public class AuthenticatedRequest : EndpointRequest<int>
		{
		}

		public class NoPolicyEndpoint : EndpointHandler<NoPolicyRequest, int>
		{
			public NoPolicyEndpoint(int value)
			{
			}

			public NoPolicyEndpoint(IHttpContextAccessor httpContextAccessor)
			{
			}

			public override ActionResult<int> HandleRequest(NoPolicyRequest request)
			{
				return 3;
			}
		}

		public class NoPolicyRequest : EndpointRequest<int>
		{
		}
	}
}