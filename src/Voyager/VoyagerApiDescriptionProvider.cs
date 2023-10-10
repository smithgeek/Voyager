using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Voyager
{
	public class PropertyMetadataRepo
	{
		public Dictionary<Type, Dictionary<string, VoyagerApiDescription.Property>> Properties = new();
	}

	public class VoyagerApiDescription
	{
		public required string AssemblyName { get; set; }
		public Type? BodyType { get; set; }
		public required string Method { get; set; }
		public required string Path { get; set; }
		public required List<Property> Properties { get; set; }
		public required string RequestTypeName { get; set; }
		public required Type ResponseType { get; set; }

		public class Property
		{
			public string? DefaultValue { get; set; }
			public string? Description { get; set; }
			public required string Name { get; set; }
			public required Type ParentType { get; set; }
			public required string PropertyName { get; set; }
			public required string Source { get; set; }
			public required Type Type { get; set; }
		}
	}

	internal class VoyagerApiDescriptionProvider : IApiDescriptionProvider
	{
		private readonly IModelMetadataProvider modelMetadataProvider;
		private readonly PropertyMetadataRepo propertyMetadatRepo;
		private readonly IEnumerable<VoyagerRouteRegistration> registrations;

		public VoyagerApiDescriptionProvider(IModelMetadataProvider modelMetadataProvider,
			List<VoyagerRouteRegistration> registrations, PropertyMetadataRepo propertyMetadatRepo)
		{
			this.modelMetadataProvider = modelMetadataProvider;
			this.registrations = registrations;
			this.propertyMetadatRepo = propertyMetadatRepo;
		}

		public int Order => 0;

		public void OnProvidersExecuted(ApiDescriptionProviderContext context)
		{
			foreach (var registration in registrations)
			{
				context.Results.Add(CreateApiDescription(registration.DescriptionFactory()));
			}
		}

		public void OnProvidersExecuting(ApiDescriptionProviderContext context)
		{
		}

		private static BindingSource GetBindingSource(string source)
		{
			return source switch
			{
				"Path" => BindingSource.Path,
				"Query" => BindingSource.Query,
				"Form" => BindingSource.Form,
				_ => BindingSource.Body,
			};
		}

		private static string GetTopRoute(string template)
		{
			if (template.Contains('/'))
			{
				return template[..template.IndexOf('/')];
			}
			return template;
		}

		private ApiDescription CreateApiDescription(VoyagerApiDescription apiDescription)
		{
			var descripton = new ApiDescription
			{
				HttpMethod = apiDescription.Method,
				ActionDescriptor = new ActionDescriptor
				{
					RouteValues = new Dictionary<string, string?>()
				},
				RelativePath = apiDescription.Path,
			};
			foreach (var property in apiDescription.Properties.Where(p => p.Source != "Body"))
			{
				var parameter = new ApiParameterDescription
				{
					Name = property.Name.ToLower(),
					Type = property.Type,
					Source = GetBindingSource(property.Source),
					ParameterDescriptor = new ParameterDescriptor
					{
						Name = property.Description ?? string.Empty,
					},
					ModelMetadata = property.ParentType != null
						? modelMetadataProvider.GetMetadataForProperty(property.ParentType, property.PropertyName)
						: modelMetadataProvider.GetMetadataForType(property.Type),
					DefaultValue = property.DefaultValue,
				};

				descripton.ParameterDescriptions.Add(parameter);
				if (!propertyMetadatRepo.Properties.ContainsKey(property.ParentType))
				{
					propertyMetadatRepo.Properties[property.ParentType] = new Dictionary<string, VoyagerApiDescription.Property>();
				}
				propertyMetadatRepo.Properties[property.ParentType][property.PropertyName] = property;
			}
			if (apiDescription.BodyType != null)
			{
				descripton.ParameterDescriptions.Add(new ApiParameterDescription
				{
					Type = apiDescription.BodyType,
					Source = BindingSource.Body,
					ModelMetadata = modelMetadataProvider.GetMetadataForType(apiDescription.BodyType)
				});
			}
			descripton.ActionDescriptor.SetProperty(apiDescription);
			descripton.ActionDescriptor.RouteValues["controller"] = GetTopRoute(apiDescription.Path);
			descripton.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });
			if (apiDescription.ResponseType != null)
			{
				var response = new ApiResponseType
				{
					StatusCode = 200,
					ModelMetadata = modelMetadataProvider.GetMetadataForType(apiDescription.ResponseType)
				};
				response.ApiResponseFormats.Add(new ApiResponseFormat { MediaType = "application/json" });
				descripton.SupportedResponseTypes.Add(response);
			}
			return descripton;
		}
	}
}