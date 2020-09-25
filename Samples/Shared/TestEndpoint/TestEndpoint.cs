using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voyager;
using Voyager.Api;

namespace Shared.TestEndpoint
{
	public class TestEndpointHandler : EndpointHandler<TestEndpointRequest, TestEndpointResponse>, IInjectHttpContext
	{
		public TestEndpointHandler()
		{
		}

		public HttpContext HttpContext { get; set; }

		public override ActionResult<TestEndpointResponse> HandleRequest(TestEndpointRequest request)
		{
			return new TestEndpointResponse { Status = "Success", Id = HttpContext.TraceIdentifier };
		}
	}
}