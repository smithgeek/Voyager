using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Voyager.Api
{
	public class JsonBodyValueProvider : IValueProvider
	{
		private readonly HttpContext httpContext;
		private readonly IOptions<JsonOptions> jsonOptions;
		private Dictionary<string, object> dictionary;

		public JsonBodyValueProvider(HttpContext httpContext, IOptions<JsonOptions> jsonOptions)
		{
			this.httpContext = httpContext;
			this.jsonOptions = jsonOptions;
		}

		public JsonValueKind? ValueKind { get; set; }

		public bool ContainsPrefix(string prefix)
		{
			return false;
		}

		public ValueProviderResult GetValue(string key)
		{
			if (dictionary == null)
			{
				var task = ParseBody();
				task.Wait();
				dictionary = task.Result;
			}
			var jsonKey = JsonNamingPolicy.CamelCase.ConvertName(key);
			var kvp = dictionary.FirstOrDefault(kvp => JsonNamingPolicy.CamelCase.ConvertName(kvp.Key) == jsonKey);
			if (kvp.Key != null)
			{
				var element = (JsonElement?)kvp.Value;
				ValueKind = element?.ValueKind;
				if (element.HasValue && element.Value.ValueKind == JsonValueKind.String)
				{
					return new ValueProviderResult(kvp.Value?.ToString());
				}
				return new ValueProviderResult(kvp.Value?.ToString());
			}
			return new ValueProviderResult();
		}

		private async Task<Dictionary<string, object>> ParseBody()
		{
			if (httpContext.Request.HasFormContentType)
			{
				return new Dictionary<string, object>();
			}
			using var reader = new StreamReader(httpContext.Request.Body);
			var body = await reader.ReadToEndAsync();
			if (string.IsNullOrWhiteSpace(body))
			{
				return new Dictionary<string, object>();
			}
			return JsonSerializer.Deserialize<Dictionary<string, object>>(body, jsonOptions.Value.JsonSerializerOptions);
		}
	}
}