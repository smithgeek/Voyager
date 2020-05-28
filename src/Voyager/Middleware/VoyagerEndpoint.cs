using Microsoft.AspNetCore.Http;

namespace Voyager.Middleware
{
	public class VoyagerEndpoint
	{
		public string Name { get; set; }
		public RequestDelegate RequestDelegate { get; set; }
	}
}