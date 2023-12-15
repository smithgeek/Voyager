using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Voyager.ModelBinding;

public class StringValuesProvider : IStringValuesProvider
{
	public StringValues GetStringValues(HttpContext context, ModelBindingSource source, string key)
	{
		return source switch
		{
			ModelBindingSource.Cookie => context.Request.Cookies.TryGetValue(key, out var value) ? value : null,

			ModelBindingSource.Route => context.Request.RouteValues.TryGetValue(key, out var value) && value is string routeString ? routeString : null,
			ModelBindingSource.Form => context.Request.Form.TryGetValue(key, out var values) ? values : StringValues.Empty,
			ModelBindingSource.Header => context.Request.Headers.TryGetValue(key, out var values) ? values : StringValues.Empty,
			ModelBindingSource.Query => context.Request.Query.TryGetValue(key, out var values) ? values : StringValues.Empty,

			_ => StringValues.Empty
		};
	}
}