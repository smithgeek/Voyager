using System;

namespace Voyager.Api
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public class VoyagerRouteAttribute : Attribute
	{
		public VoyagerRouteAttribute(HttpMethod method, string template)
			: this(Enum.GetName(typeof(HttpMethod), method), template)
		{
		}

		public VoyagerRouteAttribute(string method, string template)
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

		public VoyagerRouteDefinition ToEndpointRoute(Type type)
		{
			return new VoyagerRouteDefinition
			{
				Method = Method,
				RequestType = type,
				Template = Template
			};
		}
	}
}