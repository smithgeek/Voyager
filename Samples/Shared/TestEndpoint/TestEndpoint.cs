using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Voyager.Api;

namespace Shared.TestEndpoint;

public class TestEndpointHandler : EndpointHandler<TestEndpointRequest, TestEndpointResponse>
{
	public TestEndpointHandler()
	{
	}

	public Task<ActionResult<TestEndpointResponse>> Handle(TestEndpointRequest request, CancellationToken cancellationToken)
	{
		throw new System.NotImplementedException();
	}

	public override ActionResult<TestEndpointResponse> HandleRequest(TestEndpointRequest request)
	{
		return new TestEndpointResponse
		{
			Status = "Success",
			Id = HttpContext.TraceIdentifier,
			Message = $"{request.Other} {string.Join(", ", request.List)}"
		};
	}
}

[VoyagerEndpoint(HttpMethod.Post, "/test")]
public class TestEndpoint2
{
	public TestEndpointResponse Handle(TestEndpointRequest request)
	{
		return new TestEndpointResponse
		{
			Status = "Success",
			Id = "",
			//Id = HttpContext.TraceIdentifier,
			Message = $"{request.Other} {string.Join(", ", request.List)}"
		};
	}
}