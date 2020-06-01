using FluentAssertions;
using FluentAssertions.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Voyager.Api;
using Voyager.Api.Authorization;
using Xunit;

namespace Voyager.UnitTests.Api
{
	public class BaseEndpointTests
	{
		private readonly IServiceProvider provider;

		public BaseEndpointTests()
		{
			var services = new ServiceCollection();
			services.AddVoyager(c => c.AddAssemblyWith<BaseEndpointTests>());
			provider = services.BuildServiceProvider();
		}

		[Fact]
		public async Task AnonymousPolicyAllowsAnyone()
		{
			var scope = provider.CreateScope();
			var endpoint = GetEndpoint<AnonymousEndpoint>(scope.ServiceProvider);
			var endpoint2 = GetEndpoint<AnonymousEndpoint>();
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

		private TEndpoint GetEndpoint<TEndpoint>(IServiceProvider scopeProvider = null) where TEndpoint : InjectEndpointProps
		{
			var useProvider = scopeProvider ?? provider;
			var endpoint = useProvider.GetService<HandlerFactory<TEndpoint>>().Create();
			useProvider.GetService<IHttpContextAccessor>().HttpContext = TestFactory.HttpContext();
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