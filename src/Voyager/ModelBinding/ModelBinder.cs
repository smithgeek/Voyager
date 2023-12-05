﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Voyager.ModelBinding;

public class ModelBinder : IModelBinder
{
	private readonly HttpContext httpContext;
	private JsonElementValueProvider? bodyValueProvider;
	private FormValueProvider? formValueProvider;
	private JsonSerializerOptions? jsonOptions;
	private QueryStringValueProvider? queryStringValueProvider;
	private RouteValueProvider? routeValueProvider;

	public ModelBinder(HttpContext httpContext)
	{
		this.httpContext = httpContext;
	}

	public virtual async ValueTask<TValue?> GetBody<TValue>()
	{
		try
		{
			bodyValueProvider ??= new JsonElementValueProvider(httpContext, GetJsonOptions());
			var body = await bodyValueProvider.ParseBody<TValue>();
			return body;
		}
		catch (Exception)
		{
			return default;
		}
	}

	public virtual ValueTask<TValue?> GetCookieValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null)
	{
		if (httpContext.Request.Cookies.TryGetValue(key, out var value))
		{
			return ConvertTask(value, defaultValue);
		}
		return ValueTask.FromResult(GetDefault(defaultValue));
	}

	public virtual ValueTask<IEnumerable<TValue>> GetCookieValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null)
	{
		if (httpContext.Request.Cookies.TryGetValue(key, out var value))
		{
			var convertedValue = Convert<TValue>(value, null);
			if (convertedValue != null)
			{
				return ValueTask.FromResult(new[] { convertedValue }.AsEnumerable());
			}
		}
		return ValueTask.FromResult(GetDefaultEnumerable(defaultValue));
	}

	public virtual ValueTask<TValue?> GetFormValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null)
	{
		formValueProvider ??= new FormValueProvider(BindingSource.Form, httpContext.Request.Form, CultureInfo.InvariantCulture);
		return ConvertTask(formValueProvider.GetValue(key).FirstValue, defaultValue);
	}

	public virtual ValueTask<IEnumerable<TValue>> GetFormValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null)
	{
		formValueProvider ??= new FormValueProvider(BindingSource.Form, httpContext.Request.Form, CultureInfo.InvariantCulture);
		var values = formValueProvider.GetValue(key).Values;
		return GetEnumerableValues(values, defaultValue);
	}

	public virtual ValueTask<TValue?> GetHeaderValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null)
	{
		if (httpContext.Request.Headers.TryGetValue(key, out var value))
		{
			return ConvertTask(value.First(), defaultValue);
		}
		return ValueTask.FromResult(GetDefault(defaultValue));
	}

	public virtual ValueTask<IEnumerable<TValue>> GetHeaderValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null)
	{
		httpContext.Request.Headers.TryGetValue(key, out var values);
		return GetEnumerableValues(values, defaultValue);
	}

	public virtual ValueTask<TValue?> GetQueryStringValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null)
	{
		queryStringValueProvider ??= new QueryStringValueProvider(BindingSource.Query, httpContext.Request.Query, CultureInfo.InvariantCulture);
		return ConvertTask(queryStringValueProvider.GetValue(key).FirstValue, defaultValue);
	}

	public virtual ValueTask<IEnumerable<TValue>> GetQueryStringValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null)
	{
		queryStringValueProvider ??= new QueryStringValueProvider(BindingSource.Query, httpContext.Request.Query, CultureInfo.InvariantCulture);
		var values = queryStringValueProvider.GetValue(key).Values;
		return GetEnumerableValues(values, defaultValue);
	}

	public virtual ValueTask<TValue?> GetRouteValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null)
	{
		routeValueProvider ??= new RouteValueProvider(BindingSource.Path, httpContext.Request.RouteValues);
		return ConvertTask(routeValueProvider.GetValue(key).FirstValue, defaultValue);
	}

	public virtual ValueTask<IEnumerable<TValue>> GetRouteValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null)
	{
		routeValueProvider ??= new RouteValueProvider(BindingSource.Path, httpContext.Request.RouteValues);
		var values = routeValueProvider.GetValue(key).Values;
		return GetEnumerableValues(values, defaultValue);
	}

	private TValue? Convert<TValue>(string? value, DefaultValue<TValue>? defaultValue = null)
	{
		if (value is TValue tValue)
		{
			return tValue;
		}
		if (value is null || value == string.Empty)
		{
			return GetDefault(defaultValue);
		}
		return JsonSerializer.Deserialize<TValue>(value, GetJsonOptions());
	}

	private ValueTask<TValue?> ConvertTask<TValue>(string? value, DefaultValue<TValue>? defaultValue)
	{
		return ValueTask.FromResult(Convert(value, defaultValue));
	}

	private JsonSerializerOptions GetJsonOptions()
	{
		jsonOptions ??= httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
		return jsonOptions;
	}

	private static TValue? GetDefault<TValue>(DefaultValue<TValue>? value)
	{
		if (value == null)
		{
			return default;
		}
		return value.Value;
	}

	private static IEnumerable<TValue> GetDefaultEnumerable<TValue>(DefaultValue<IEnumerable<TValue>>? value)
	{
		if (value == null)
		{
			return Enumerable.Empty<TValue>();
		}
		return value.Value;
	}

	private ValueTask<IEnumerable<TValue>> GetEnumerableValues<TValue>(StringValues stringValues, DefaultValue<IEnumerable<TValue>>? defaultValue)
	{
		if (stringValues.Any())
		{
			return ValueTask.FromResult(stringValues.Select(v => Convert<TValue>(v, null)).WhereNotNull());
		}
		return ValueTask.FromResult(GetDefaultEnumerable(defaultValue));
	}

}