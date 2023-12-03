using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Voyager.ModelBinding;

internal class JsonElementValueProvider
{
	private readonly HttpContext httpContext;
	private readonly JsonSerializerOptions jsonOptions;
	private Dictionary<string, JsonElement>? dictionary = null;

	public JsonElementValueProvider(HttpContext httpContext, JsonSerializerOptions jsonOptions)
	{
		this.httpContext = httpContext;
		this.jsonOptions = jsonOptions;
	}

	public async Task<TValue?> GetValue<TValue>(string key)
	{
		dictionary ??= await ParseBody();
		if (dictionary != null)
		{
			var jsonKey = JsonNamingPolicy.CamelCase.ConvertName(key);
			var kvp = dictionary.FirstOrDefault(kvp => JsonNamingPolicy.CamelCase.ConvertName(kvp.Key) == jsonKey);
			if (kvp.Key != null)
			{
				return kvp.Value.Deserialize<TValue>() ?? default;
			}
		}
		return default;
	}

	public ValueTask<TValue?> ParseBody<TValue>()
	{
		return JsonSerializer.DeserializeAsync<TValue>(httpContext.Request.Body, jsonOptions);
	}

	private async ValueTask<Dictionary<string, JsonElement>?> ParseBody()
	{
		if (httpContext.Request.HasFormContentType
			|| httpContext.Request.ContentLength == null
			|| httpContext.Request.ContentLength < 1)
		{
			return null;
		}
		return await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(httpContext.Request.Body,
			jsonOptions);
	}
}