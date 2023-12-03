using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Shared.TestEndpoint
{
	public class TestEndpointRequest
	{
		public required string Item1 { get; init; }

		public required int Item2 { get; init; }

		[FromQuery(Name = "abc")]
		public required IEnumerable<int> List { get; init; }

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