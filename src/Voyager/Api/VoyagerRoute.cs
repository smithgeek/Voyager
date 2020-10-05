using System;

namespace Voyager.Api
{
	public class VoyagerRouteDefinition
	{
		public string Method { get; set; }
		public Type RequestType { get; set; }
		public string Template { get; set; }
	}
}