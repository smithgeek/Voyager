using System;

namespace Voyager.Api
{
	public class VoyagerRoute
	{
		public string Method { get; set; }
		public Type RequestType { get; set; }
		public string Template { get; set; }
	}
}