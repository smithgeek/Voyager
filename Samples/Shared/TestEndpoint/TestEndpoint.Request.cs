using Voyager.Api;

namespace Shared.TestEndpoint
{
	[VoyagerRoute(HttpMethod.Post, "/test")]
	public class TestEndpointRequest : EndpointRequest<TestEndpointResponse>
	{
		[FromBody("abc")]
		public required int NotUsed { get; init; }

		public required string Other { get; init; }
	}

	public class TestEndpointResponse
	{
		public required string Id { get; set; }
		public required string Message { get; init; }
		public required string Status { get; set; }
	}
}