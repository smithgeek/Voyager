using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using System;

namespace Voyager.Api
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public class RouteAttribute : Attribute
	{
		public RouteAttribute(HttpMethod method, string template)
		{
			Template = template;
			TemplateMatcher = GetTemplateMatcher(template);
			Method = Enum.GetName(typeof(HttpMethod), method).ToUpperInvariant();
		}

		public RouteAttribute(string method, string template)
		{
			Template = template;
			TemplateMatcher = GetTemplateMatcher(template);
			Method = method.ToUpperInvariant();
		}

		public string Method { get; }

		public string Template { get; set; }
		public TemplateMatcher TemplateMatcher { get; }

		public EndpointRoute ToEndpointRoute(Type type)
		{
			return new EndpointRoute
			{
				Method = Method,
				TemplateMatcher = TemplateMatcher,
				RequestType = type,
				Template = Template
			};
		}

		private static RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate)
		{
			var result = new RouteValueDictionary();

			foreach (var parameter in parsedTemplate.Parameters)
			{
				if (parameter.DefaultValue != null)
				{
					result.Add(parameter.Name, parameter.DefaultValue);
				}
			}

			return result;
		}

		private TemplateMatcher GetTemplateMatcher(string templateString)
		{
			var template = TemplateParser.Parse(templateString);
			return new TemplateMatcher(template, GetDefaults(template));
		}
	}
}