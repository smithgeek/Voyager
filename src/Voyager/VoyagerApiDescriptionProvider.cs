using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using Voyager.Api;

namespace Voyager
{
	public class OpenApiMetadata
	{
		public string? AssemblyName { get; set; }
		public string? TypeFullName { get; set; }
	}

	internal class VoyagerApiDescriptionProvider : IApiDescriptionProvider
	{
		private readonly IModelMetadataProvider modelMetadataProvider;
		private readonly TypeBindingRepository typeBindingRepo;
		private readonly IEnumerable<VoyagerRouteDefinition> voyagerRoutes;

		public VoyagerApiDescriptionProvider(IEnumerable<VoyagerRouteDefinition> voyagerRoutes, IModelMetadataProvider modelMetadataProvider, TypeBindingRepository typeBindingRepo)
		{
			this.voyagerRoutes = voyagerRoutes;
			this.modelMetadataProvider = modelMetadataProvider;
			this.typeBindingRepo = typeBindingRepo;
		}

		public int Order => 0;

		public void OnProvidersExecuted(ApiDescriptionProviderContext context)
		{
			var typeProvider = new DynamicTypeBuilder(typeBindingRepo);
			foreach (var route in voyagerRoutes)
			{
				var path = $"{route.Template}";
				var descriptor = new ApiDescription
				{
					HttpMethod = route.Method,
					ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor
					{
						RouteValues = new Dictionary<string, string?>()
					},
					RelativePath = path,
				};
				var validBindingSources = new[] { BindingSource.Form, BindingSource.Query, BindingSource.Path };
				foreach (var property in typeBindingRepo.GetProperties(route.RequestType))
				{
					if (validBindingSources.Contains(property.BindingSource))
					{
						descriptor.ParameterDescriptions.Add(new ApiParameterDescription
						{
							Name = property.Name.ToLower(),
							Type = property.Property.PropertyType,
							Source = property.BindingSource ?? BindingSource.Custom,
							ParameterDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor { Name = property.Description }
						});
					}
				}

				var requestBodyType = typeProvider.CreateBodyType(route.RequestType);
				if (requestBodyType != null)
				{
					var requestModel = modelMetadataProvider.GetMetadataForType(requestBodyType);
					descriptor.ParameterDescriptions.Add(new ApiParameterDescription
					{
						Type = requestBodyType,
						Source = BindingSource.Body,
						ModelMetadata = requestModel
					});
				}
				descriptor.ActionDescriptor.SetProperty(new OpenApiMetadata
				{
					AssemblyName = route.RequestType.Assembly.GetName().Name,
					TypeFullName = route.RequestType.FullName
				});
				descriptor.ActionDescriptor.RouteValues["controller"] = GetTopRoute(path);
				descriptor.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });
				var responseType = GetResponseType(route.RequestType);
				if (responseType != null)
				{
					var response = new ApiResponseType
					{
						StatusCode = 200,
						ModelMetadata = modelMetadataProvider.GetMetadataForType(responseType)
					};
					response.ApiResponseFormats.Add(new ApiResponseFormat { MediaType = "application/json" });
					descriptor.SupportedResponseTypes.Add(response);
				}
				context.Results.Add(descriptor);
			}
		}

		public void OnProvidersExecuting(ApiDescriptionProviderContext context)
		{
		}

		private Type? GetResponseType(Type requestType)
		{
			var request = requestType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
			if (request != null)
			{
				var returnType = request.GetGenericArguments()[0];
				if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ActionResult<>))
				{
					return returnType.GetGenericArguments()[0];
				}
			}
			return null;
		}

		private string GetTopRoute(string template)
		{
			if (template.Contains("/"))
			{
				return template.Substring(0, template.IndexOf('/'));
			}
			return template;
		}
	}
}