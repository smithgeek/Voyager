using System;

namespace Voyager.Api
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public class RouteAttribute : Attribute
	{
		public RouteAttribute(HttpMethod method, string template)
			: this(Enum.GetName(typeof(HttpMethod), method), template)
		{
		}

		public RouteAttribute(string method, string template)
		{
			if (template.StartsWith("/"))
			{
				template = template.Substring(1);
			}
			Template = template;
			Method = method.ToUpperInvariant();
		}

		public string Method { get; }

		public string Template { get; set; }

		public VoyagerRoute ToEndpointRoute(Type type)
		{
			return new VoyagerRoute
			{
				Method = Method,
				RequestType = type,
				Template = Template
			};
		}
	}
}