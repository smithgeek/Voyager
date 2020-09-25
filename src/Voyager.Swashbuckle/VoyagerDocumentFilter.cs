using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Voyager.Swashbuckle
{
	public class VoyagerDocumentFilter : IDocumentFilter
	{
		private readonly VoyagerOptionsHolder voyagerOptionsHolder;

		public VoyagerDocumentFilter(VoyagerOptionsHolder voyagerOptionsHolder)
		{
			this.voyagerOptionsHolder = voyagerOptionsHolder;
		}

		public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
		{
			var tags = swaggerDoc.Paths.SelectMany(path => path.Value.Operations.SelectMany(op => op.Value.Tags));
			swaggerDoc.Tags = tags.GroupBy(t => t.Name).Select(group => group.First()).ToList();
			if (!string.IsNullOrEmpty(voyagerOptionsHolder.MapOptions.Prefix))
			{
				swaggerDoc.Servers.Add(new OpenApiServer() { Url = $"/{voyagerOptionsHolder.MapOptions.Prefix}" });
			}
		}
	}
}