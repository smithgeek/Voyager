using Voyager.Api;

namespace Shared.TestEndpoint
{
	[VoyagerRoute(HttpMethod.Get, "/test")]
	public class TestEndpointRequest : EndpointRequest<TestEndpointResponse>
	{
		public int NotUsed { get; set; }
	}

	public class TestEndpointResponse
	{
		public string Id { get; set; }
		public string Status { get; set; }
	}
}