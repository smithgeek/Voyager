using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Voyager.Api
{
	public class JsonBodyValueProvider : IValueProvider
	{
		private readonly Dictionary<string, object> dictionary;

		public JsonBodyValueProvider(Dictionary<string, object> dictionary)
		{
			this.dictionary = dictionary;
		}

		public bool ContainsPrefix(string prefix)
		{
			return false;
		}

		public ValueProviderResult GetValue(string key)
		{
			var jsonKey = JsonNamingPolicy.CamelCase.ConvertName(key);
			var kvp = dictionary.FirstOrDefault(kvp => JsonNamingPolicy.CamelCase.ConvertName(kvp.Key) == jsonKey);
			if (kvp.Key != null)
			{
				return new ValueProviderResult(kvp.Value?.ToString());
			}
			return new ValueProviderResult();
		}
	}
}