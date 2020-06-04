using FluentAssertions;
using FluentAssertions.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Voyager.Api;
using Voyager.Api.Authorization;
using Xunit;

namespace Voyager.UnitTests.Api
{
	public class AuthorizationTests
	{
		private readonly IMediator mediator;
		private readonly IServiceProvider provider;

		public AuthorizationTests()
		{
			var services = new ServiceCollection();
			services.AddVoyager(c => c.AddAssemblyWith<AuthorizationTests>());
			provider = services.BuildServiceProvider();
			mediator = provider.GetRequiredService<IMediator>();
			provider.GetService<IHttpContextAccessor>().HttpContext = TestFactory.HttpContext();
		}

		[Fact]
		public async Task AnonymousPolicyAllowsAnyone()
		{
			var response = await mediator.Send(new AnonymousRequest());
			response.Value.Should().Be(3);
		}

		[Fact]
		public async Task NoPolciyAllowsAnyone()
		{
			var response = await mediator.Send(new NoPolicyRequest());
			response.Value.Should().Be(3);
		}

		[Fact]
		public async Task PolicyIsEnforced()
		{
			var response = await mediator.Send(new AuthenticatedRequest());
			response.Result.Should().BeUnauthorizedResult();
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