using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Voyager.Swashbuckle
{
	public class VoyagerOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			var metadata = context.ApiDescription.ActionDescriptor.GetProperty<VoyagerApiDescription>();
			if (metadata != null)
			{
				operation.OperationId = metadata.RequestTypeName.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
				if (operation.OperationId?.EndsWith("Request") ?? false)
				{
					operation.OperationId = operation.OperationId.Replace("Request", "");
				}
				operation.Summary = ToSentenceCase(operation.OperationId);
				var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{metadata.AssemblyName}.xml");
				if (File.Exists(xmlPath))
				{
					var doc = XDocument.Load(xmlPath);
					var member = doc.Root?.XPathSelectElement($"/doc/members/member[@name=\"T:{metadata.RequestTypeName}\"]");
					operation.Description = member?.XPathSelectElement("summary")?.Value.Trim() ?? " ";
				}
			}
			foreach (var tag in operation.Tags)
			{
				if (!string.IsNullOrWhiteSpace(tag.Name))
				{
					tag.Name = $"{char.ToUpper(tag.Name[0])}{tag.Name.Substring(1)}";
				}
			}
		}

		private static string ToSentenceCase(string str)
		{
			return Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");
		}
	}
}