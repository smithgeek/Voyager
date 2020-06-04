using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Voyager.Api;

namespace Voyager
{
	public class VoyagerApiDescriptionProvider : IApiDescriptionProvider
	{
		private readonly IModelMetadataProvider modelMetadataProvider;
		private readonly TypeBindingRepository typeBindingRepo;
		private readonly IEnumerable<VoyagerRoute> voyagerRoutes;

		public VoyagerApiDescriptionProvider(IEnumerable<VoyagerRoute> voyagerRoutes, IModelMetadataProvider modelMetadataProvider, TypeBindingRepository typeBindingRepo)
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
				var descriptor = new ApiDescription
				{
					HttpMethod = route.Method,
					ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor
					{
						RouteValues = new Dictionary<string, string>()
					},
					RelativePath = route.Template,
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
							Source = property.BindingSource,
							ParameterDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor { Name = property.Description }
						});
					}
				}

				var requestBodyType = typeProvider.CreateBodyType(route.RequestType);
				var requestModel = modelMetadataProvider.GetMetadataForType(requestBodyType);
				descriptor.ParameterDescriptions.Add(new ApiParameterDescription
				{
					Type = requestBodyType,
					Source = BindingSource.Body,
					ModelMetadata = requestModel
				});
				descriptor.ActionDescriptor.RouteValues["controller"] = GetTopRoute(route.Template);
				descriptor.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });
				var responseType = GetResponseType(route.RequestType);
				if (responseType != null)
				{
					var response = new ApiResponseType();
					response.ApiResponseFormats.Add(new ApiResponseFormat { MediaType = "application/json" });
					response.ModelMetadata = modelMetadataProvider.GetMetadataForType(responseType);
					descriptor.SupportedResponseTypes.Add(response);
				}
				context.Results.Add(descriptor);
			}
		}

		public void OnProvidersExecuting(ApiDescriptionProviderContext context)
		{
		}

		private Type GetResponseType(Type requestType)
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

	internal class DynamicTypeBuilder
	{
		private readonly ModuleBuilder moduleBuilder;
		private readonly TypeBindingRepository typeBindingRepo;

		public DynamicTypeBuilder(TypeBindingRepository typeBindingRepo)
		{
			this.typeBindingRepo = typeBindingRepo;
			var assemblyName = new AssemblyName("Voyager.OpenApi.Types");
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			moduleBuilder = assemblyBuilder.DefineDynamicModule("Types");
		}

		public Type CreateBodyType(Type type)
		{
			var typeBuilder = GetRequestBodyTypeBuilder(type);
			typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			foreach (var property in typeBindingRepo.GetProperties(type))
			{
				if (property.BindingSource == BindingSource.Body)
				{
					CreateProperty(typeBuilder, property.Name, property.Property.PropertyType);
				}
			}
			return typeBuilder.CreateTypeInfo().AsType();
		}

		private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
		{
			var fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

			var propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
			var getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
			var getIl = getPropMthdBldr.GetILGenerator();

			getIl.Emit(OpCodes.Ldarg_0);
			getIl.Emit(OpCodes.Ldfld, fieldBuilder);
			getIl.Emit(OpCodes.Ret);

			var setPropMthdBldr =
				tb.DefineMethod("set_" + propertyName,
				  MethodAttributes.Public |
				  MethodAttributes.SpecialName |
				  MethodAttributes.HideBySig,
				  null, new[] { propertyType });

			var setIl = setPropMthdBldr.GetILGenerator();
			var modifyProperty = setIl.DefineLabel();
			var exitSet = setIl.DefineLabel();

			setIl.MarkLabel(modifyProperty);
			setIl.Emit(OpCodes.Ldarg_0);
			setIl.Emit(OpCodes.Ldarg_1);
			setIl.Emit(OpCodes.Stfld, fieldBuilder);

			setIl.Emit(OpCodes.Nop);
			setIl.MarkLabel(exitSet);
			setIl.Emit(OpCodes.Ret);

			propertyBuilder.SetGetMethod(getPropMthdBldr);
			propertyBuilder.SetSetMethod(setPropMthdBldr);
		}

		private TypeBuilder GetRequestBodyTypeBuilder(Type type)
		{
			var typeSignature = $"{type.Name}RequestBody";
			var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null);
			return typeBuilder;
		}
	}
}