using System;

namespace Voyager.Api
{
	public class VoyagerRouteDefinition
	{
		public required string Method { get; init; }
		public required Type RequestType { get; init; }
		public required string Template { get; init; }
	}
}