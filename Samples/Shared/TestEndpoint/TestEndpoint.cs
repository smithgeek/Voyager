using Microsoft.AspNetCore.Mvc;
using Voyager.Api;

namespace Shared.TestEndpoint
{
	public class TestEndpointHandler : EndpointHandler<TestEndpointRequest, TestEndpointResponse>
	{
		public TestEndpointHandler()
		{
		}

		public override ActionResult<TestEndpointResponse> HandleRequest(TestEndpointRequest request)
		{
			return new TestEndpointResponse { Status = "Success" };
		}
	}
}