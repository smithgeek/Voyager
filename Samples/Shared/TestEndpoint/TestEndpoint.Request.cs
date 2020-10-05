using Voyager.Api;

namespace Shared.TestEndpoint
{
	[VoyagerRoute(HttpMethod.Get, "/test")]
	public class TestEndpointRequest : EndpointRequest<TestEndpointResponse>
	{
	}

	public class TestEndpointResponse
	{
		public string Id { get; set; }
		public string Status { get; set; }
	}
}