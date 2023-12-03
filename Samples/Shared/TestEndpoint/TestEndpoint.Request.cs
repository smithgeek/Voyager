using System.Collections.Generic;
using Voyager.Api;

namespace Shared.TestEndpoint
{
	//[VoyagerRoute(HttpMethod.Post, "/test")]
	public class TestEndpointRequest : EndpointRequest<TestEndpointResponse>
	{
		public required string Item1 { get; init; }

		public required int Item2 { get; init; }

		[FromQuery("abc")]
		public required List<int> List { get; init; }

		[FromQuery]
		public required string Other { get; init; }
	}

	public class TestEndpointResponse
	{
		public required string Id { get; set; }
		public required string Message { get; init; }
		public required string Status { get; set; }
	}
}