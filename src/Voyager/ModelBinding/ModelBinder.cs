using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

	public virtual ValueTask<TValue?> GetBody<TValue>()
	{
		bodyValueProvider ??= new JsonElementValueProvider(httpContext, GetJsonOptions());
		return bodyValueProvider.ParseBody<TValue>();
	}

	public virtual async ValueTask<TValue?> GetBodyValue<TValue>(string key)
	{
		bodyValueProvider ??= new JsonElementValueProvider(httpContext, GetJsonOptions());
		return await bodyValueProvider.GetValue<TValue>(key);
	}

	public virtual ValueTask<TValue?> GetCookieValue<TValue>(string key)
	{
		if (httpContext.Request.Cookies.TryGetValue(key, out var value))
		{
			return ConvertTask<TValue>(value);
		}
		return default;
	}

	public virtual ValueTask<IEnumerable<TValue?>> GetCookieValues<TValue>(string key)
	{
		if (httpContext.Request.Cookies.TryGetValue(key, out var value))
		{
			return ValueTask.FromResult(new[] { Convert<TValue>(value) }.AsEnumerable());
		}
		return default;
	}

	public virtual ValueTask<TValue?> GetFormValue<TValue>(string key)
	{
		formValueProvider ??= new FormValueProvider(BindingSource.Form, httpContext.Request.Form, CultureInfo.InvariantCulture);
		return ConvertTask<TValue>(formValueProvider.GetValue(key).FirstValue);
	}

	public virtual ValueTask<IEnumerable<TValue?>> GetFormValues<TValue>(string key)
	{
		formValueProvider ??= new FormValueProvider(BindingSource.Form, httpContext.Request.Form, CultureInfo.InvariantCulture);
		return ValueTask.FromResult(formValueProvider.GetValue(key).Values.Select(Convert<TValue>));
	}

	public virtual ValueTask<TValue?> GetHeaderValue<TValue>(string key)
	{
		if (httpContext.Request.Headers.TryGetValue(key, out var value))
		{
			return ConvertTask<TValue>(value.First());
		}
		return default;
	}

	public virtual ValueTask<IEnumerable<TValue?>> GetHeaderValues<TValue>(string key)
	{
		if (httpContext.Request.Headers.TryGetValue(key, out var values))
		{
			return ValueTask.FromResult(values.Select(Convert<TValue>));
		}
		return default;
	}

	public virtual ValueTask<TValue?> GetQueryStringValue<TValue>(string key)
	{
		queryStringValueProvider ??= new QueryStringValueProvider(BindingSource.Query, httpContext.Request.Query, CultureInfo.InvariantCulture);
		return ConvertTask<TValue>(queryStringValueProvider.GetValue(key).FirstValue);
	}

	public virtual ValueTask<IEnumerable<TValue?>> GetQueryStringValues<TValue>(string key)
	{
		queryStringValueProvider ??= new QueryStringValueProvider(BindingSource.Query, httpContext.Request.Query, CultureInfo.InvariantCulture);
		return ValueTask.FromResult(queryStringValueProvider.GetValue(key).Values.Select(Convert<TValue>));
	}

	public virtual ValueTask<TValue?> GetRouteValue<TValue>(string key)
	{
		routeValueProvider ??= new RouteValueProvider(BindingSource.Path, httpContext.Request.RouteValues);
		return ConvertTask<TValue>(routeValueProvider.GetValue(key).FirstValue);
	}

	public virtual ValueTask<IEnumerable<TValue?>> GetRouteValues<TValue>(string key)
	{
		routeValueProvider ??= new RouteValueProvider(BindingSource.Path, httpContext.Request.RouteValues);
		return ValueTask.FromResult(routeValueProvider.GetValue(key).Values.Select(Convert<TValue>));
	}

	private TValue? Convert<TValue>(string? value)
	{
		if (value is TValue tValue)
		{
			return tValue;
		}
		if (value is null || value == string.Empty)
		{
			return default;
		}
		return JsonSerializer.Deserialize<TValue>(value, GetJsonOptions());
	}

	private ValueTask<TValue?> ConvertTask<TValue>(string? value)
	{
		return ValueTask.FromResult(Convert<TValue>(value));
	}

	private JsonSerializerOptions GetJsonOptions()
	{
		jsonOptions ??= httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
		return jsonOptions;
	}
}