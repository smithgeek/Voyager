using Voyager.Api;

namespace Shared.TestEndpoint
{
	[Route(HttpMethod.Get, "/test")]
	public class TestEndpointRequest : EndpointRequest<TestEndpointResponse>
	{
	}

	public class TestEndpointResponse
	{
		public string Status { get; set; }
	}
}