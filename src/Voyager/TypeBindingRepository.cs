using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Voyager
{
	public class TypeBindingRepository
	{
		private readonly Dictionary<Type, IEnumerable<BoundProperty>> cache = new Dictionary<Type, IEnumerable<BoundProperty>>();

		public IEnumerable<BoundProperty> GetProperties(Type type)
		{
			lock (cache)
			{
				if (cache.ContainsKey(type))
				{
					return cache[type];
				}
				else
				{
					var properties = type.GetProperties().Select(GetBoundProperty);
					cache[type] = properties;
					return properties;
				}
			}
		}

		private BoundProperty GetBoundProperty(PropertyInfo property)
		{
			var boundProp = new BoundProperty
			{
				Property = property,
				Description = property.Name,
				PropertyName = property.Name,
				Name = GetCamelCase(property.Name)
			};
			var routeAttr = property.GetCustomAttribute<Api.FromRouteAttribute>();
			if (routeAttr != null)
			{
				boundProp.Name = routeAttr.Name ?? GetCamelCase(property.Name);
				boundProp.BindingSource = BindingSource.Path;
				return boundProp;
			}

			var queryAttr = property.GetCustomAttribute<Api.FromQueryAttribute>();
			if (queryAttr != null)
			{
				boundProp.Name = queryAttr.Name ?? GetCamelCase(property.Name);
				boundProp.BindingSource = BindingSource.Query;
				return boundProp;
			}

			var formAttr = property.GetCustomAttribute<Api.FromFormAttribute>();
			if (formAttr != null)
			{
				boundProp.Name = formAttr.Name ?? GetCamelCase(property.Name);
				boundProp.BindingSource = BindingSource.Form;
				return boundProp;
			}
			return boundProp;
		}

		private string GetCamelCase(string value)
		{
			return char.ToLower(value[0]) + value.Substring(1);
		}
	}
}