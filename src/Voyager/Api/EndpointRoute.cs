using Microsoft.AspNetCore.Routing.Template;
using System;

namespace Voyager.Api
{
	public class EndpointRoute
	{
		public string Method { get; set; }
		public Type RequestType { get; set; }
		public TemplateMatcher TemplateMatcher { get; set; }
		public string Template { get; set; }
	}
}