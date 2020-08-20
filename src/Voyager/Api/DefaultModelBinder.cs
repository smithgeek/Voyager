using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Voyager.SetProperties;

namespace Voyager.Api
{
	public class DefaultModelBinder : ModelBinder
	{
		private readonly IOptions<JsonOptions> jsonOptions;
		private readonly PropertySetterFactory propertySetterFactory;
		private readonly TypeBindingRepository typeBindingRepo;

		public DefaultModelBinder(PropertySetterFactory propertySetterFactory, TypeBindingRepository typeBindingRepo, IOptions<JsonOptions> jsonOptions)
		{
			this.propertySetterFactory = propertySetterFactory;
			this.typeBindingRepo = typeBindingRepo;
			this.jsonOptions = jsonOptions;
		}

		public async Task<TRequest> Bind<TRequest>(HttpContext context)
		{
			return (TRequest)await BindInternal(context, typeof(TRequest));
		}

		public async Task<TRequest> Bind<TRequest, TResponse>(HttpContext context)
		{
			return (TRequest)await BindInternal(context, typeof(TRequest));
		}

		public Task<object> Bind(HttpContext context, Type returnType)
		{
			return BindInternal(context, returnType);
		}

		private async Task<object> BindInternal(HttpContext context, Type returnType)
		{
			var mediatorRequest = await ParseBody(context, returnType);
			var routeParams = context.Request.RouteValues;
			var queryString = context.Request.Query;
			var form = context.Request.HasFormContentType ? context.Request.Form : null;
			foreach (var property in typeBindingRepo.GetProperties(returnType))
			{
				if (property.BindingSource == BindingSource.Path)
				{
					GetFromRoute(mediatorRequest, routeParams, property);
					continue;
				}
				if (property.BindingSource == BindingSource.Query)
				{
					GetFromQuery(mediatorRequest, queryString, property);
					continue;
				}
				if (form != null && property.BindingSource == BindingSource.Form)
				{
					GetFromForm(mediatorRequest, form, property);
				}
			}
			return mediatorRequest;
		}

		private void GetFromForm<TRequest>(TRequest mediatorRequest, IFormCollection form, BoundProperty property)
		{
			if (typeof(IEnumerable<IFormFile>).IsAssignableFrom(property.Property.PropertyType))
			{
				property.Property.SetValue(mediatorRequest, form.Files ?? (IFormFileCollection)new List<IFormFile>());
			}
			else
			{
				if (form.ContainsKey(property.Name))
				{
					GetSetter(property.Property).SetValue(property.Property, mediatorRequest, form[property.Name].ToString());
				}
			}
		}

		private void GetFromQuery<TRequest>(TRequest mediatorRequest, IQueryCollection queryString, BoundProperty property)
		{
			if (queryString.ContainsKey(property.Name))
			{
				GetSetter(property.Property).SetValue(property.Property, mediatorRequest, queryString[property.Name].ToString());
			}
		}

		private void GetFromRoute<TRequest>(TRequest mediatorRequest, RouteValueDictionary routeParams, BoundProperty property)
		{
			if (routeParams.ContainsKey(property.Name))
			{
				GetSetter(property.Property).SetValue(property.Property, mediatorRequest, routeParams[property.Name].ToString());
			}
		}

		private SetPropertyValue GetSetter(PropertyInfo property)
		{
			return propertySetterFactory.Get(property.PropertyType);
		}

		private async Task<object> ParseBody(HttpContext context, Type returnType)
		{
			if (context.Request.HasFormContentType)
			{
				return Activator.CreateInstance(returnType);
			}
			using var reader = new StreamReader(context.Request.Body);
			var body = await reader.ReadToEndAsync();
			if (string.IsNullOrWhiteSpace(body))
			{
				return Activator.CreateInstance(returnType);
			}
			jsonOptions.Value.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			return JsonSerializer.Deserialize(body, returnType, jsonOptions.Value.JsonSerializerOptions);
		}
	}
}