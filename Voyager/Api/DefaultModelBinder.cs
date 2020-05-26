using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
		private readonly PropertySetterFactory propertySetterFactory;

		public DefaultModelBinder(PropertySetterFactory propertySetterFactory)
		{
			this.propertySetterFactory = propertySetterFactory;
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
			foreach (var property in returnType.GetProperties())
			{
				var fromRoute = property.GetCustomAttribute<FromRouteAttribute>();
				if (fromRoute != null)
				{
					GetFromRoute(mediatorRequest, routeParams, property, fromRoute);
				}
				else
				{
					var fromQuery = property.GetCustomAttribute<FromQueryAttribute>();
					if (fromQuery != null)
					{
						GetFromQuery(mediatorRequest, queryString, property, fromQuery);
					}
					else if (form != null)
					{
						GetFromForm(mediatorRequest, form, property);
					}
				}
			}
			return mediatorRequest;
		}

		private string GetCamelCase(string value)
		{
			return char.ToLower(value[0]) + value.Substring(1);
		}

		private void GetFromForm<TRequest>(TRequest mediatorRequest, IFormCollection form, PropertyInfo property)
		{
			var fromForm = property.GetCustomAttribute<FromFormAttribute>();
			if (fromForm != null)
			{
				if (typeof(IEnumerable<IFormFile>).IsAssignableFrom(property.PropertyType))
				{
					property.SetValue(mediatorRequest, form.Files ?? (IFormFileCollection)new List<IFormFile>());
				}
				else
				{
					var name = fromForm.Name ?? GetCamelCase(property.Name);
					if (form.ContainsKey(name))
					{
						GetSetter(property).SetValue(property, mediatorRequest, form[name].ToString());
					}
				}
			}
		}

		private void GetFromQuery<TRequest>(TRequest mediatorRequest, IQueryCollection queryString, PropertyInfo property, FromQueryAttribute fromQuery)
		{
			var name = fromQuery.Name ?? GetCamelCase(property.Name);
			if (queryString.ContainsKey(name))
			{
				GetSetter(property).SetValue(property, mediatorRequest, queryString[name].ToString());
			}
		}

		private void GetFromRoute<TRequest>(TRequest mediatorRequest, RouteValueDictionary routeParams, PropertyInfo property, FromRouteAttribute fromRoute)
		{
			var name = fromRoute.Name ?? GetCamelCase(property.Name);
			if (routeParams.ContainsKey(name))
			{
				GetSetter(property).SetValue(property, mediatorRequest, routeParams[name].ToString());
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
			return JsonSerializer.Deserialize(body, returnType, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
		}
	}
}