using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Voyager.Api
{
	public class DataProvider
	{
		private readonly HttpContext httpContext;
		private JsonElementValueProvider? bodyValueProvider;
		private FormValueProvider? formValueProvider;
		private JsonSerializerOptions? jsonOptions;
		private QueryStringValueProvider? queryStringValueProvider;
		private RouteValueProvider? routeValueProvider;

		public DataProvider(HttpContext httpContext)
		{
			this.httpContext = httpContext;
		}

		public ValueTask<TValue?> GetBody<TValue>()
		{
			bodyValueProvider ??= new JsonElementValueProvider(httpContext, GetJsonOptions());
			return bodyValueProvider.ParseBody<TValue>();
		}

		public async Task<TValue?> GetBodyValue<TValue>(string key)
		{
			bodyValueProvider ??= new JsonElementValueProvider(httpContext, GetJsonOptions());
			return await bodyValueProvider.GetValue<TValue>(key);
		}

		public Task<TValue?> GetFormValue<TValue>(string key)
		{
			formValueProvider ??= new FormValueProvider(BindingSource.Form, httpContext.Request.Form, CultureInfo.InvariantCulture);
			return Convert<TValue>(formValueProvider.GetValue(key).FirstValue ?? string.Empty);
		}

		public Task<TValue?> GetQueryStringValue<TValue>(string key)
		{
			queryStringValueProvider ??= new QueryStringValueProvider(BindingSource.Query, httpContext.Request.Query, CultureInfo.InvariantCulture);
			return Convert<TValue>(queryStringValueProvider.GetValue(key).FirstValue);
		}

		public Task<TValue?> GetRouteValue<TValue>(string key)
		{
			routeValueProvider ??= new RouteValueProvider(BindingSource.Path, httpContext.Request.RouteValues);
			return Convert<TValue>(routeValueProvider.GetValue(key).FirstValue);
		}

		private Task<TValue?> Convert<TValue>(string? value)
		{
			if (value is TValue tValue)
			{
				return Task.FromResult<TValue?>(tValue);
			}
			if (value is null || value == string.Empty)
			{
				return Task.FromResult(default(TValue?));
			}
			return Task.FromResult(JsonSerializer.Deserialize<TValue>(value, GetJsonOptions()));
		}

		private JsonSerializerOptions GetJsonOptions()
		{
			jsonOptions ??= httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
			return jsonOptions;
		}
	}
}