using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading;
using Voyager;

namespace Shared.TestEndpoint;

[VoyagerEndpoint("/test")]
public class TestEndpointHandler
{
	public required HttpContext HttpContext { get; set; }

	public TestEndpointResponse Post(TestEndpointRequest request)
	{
		return new TestEndpointResponse
		{
			Status = "Success",
			Id = HttpContext.TraceIdentifier,
			Message = $"{request.Other} {string.Join(", ", request.List)}"
		};
	}
}

[VoyagerEndpoint("/test2")]
public class TestEndpoint2
{
	private readonly IHttpContextAccessor httpContextAccessor;

	public TestEndpoint2(IHttpContextAccessor httpContextAccessor)
	{
		this.httpContextAccessor = httpContextAccessor;
	}

	public required CancellationToken CancellationToken { get; set; }
	public required HttpContext HttpContext { get; set; }

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder.WithDescription("Some description");
	}

	public TestEndpointResponse Post(TestEndpointRequest request, HttpContext context3, CancellationToken cancel2)
	{
		return new TestEndpointResponse
		{
			Status = "Success",
			Id = HttpContext.TraceIdentifier,
			Message = $"{request.Other} {string.Join(", ", request.List)}"
		};
	}
}