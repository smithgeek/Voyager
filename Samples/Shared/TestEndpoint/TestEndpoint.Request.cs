using Voyager.Api;

namespace Shared.TestEndpoint
{
	[Route(HttpMethod.Get, "/test")]
	public class TestEndpointRequest : EndpointRequest<TestEndpointResponse>
	{
	}

	public class TestEndpointResponse
	{
		public string Id { get; set; }
		public string Status { get; set; }
	}
}