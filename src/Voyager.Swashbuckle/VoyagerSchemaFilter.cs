using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Voyager.Swashbuckle
{
	public class VoyagerSchemaFilter : ISchemaFilter
	{
		private readonly PropertyMetadataRepo metadataRepo;

		public VoyagerSchemaFilter(PropertyMetadataRepo metadataRepo)
		{
			this.metadataRepo = metadataRepo;
		}

		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			if (context.MemberInfo?.DeclaringType != null)
			{
				if (metadataRepo.Properties.TryGetValue(context.MemberInfo.DeclaringType, out var properties))
				{
					if (properties.TryGetValue(context.MemberInfo.Name, out var property))
					{
						schema.Default = OpenApiAnyFactory.CreateFromJson(property.DefaultValue);
						schema.Description = property.Description;
					}
				}
			}
		}
	}
}