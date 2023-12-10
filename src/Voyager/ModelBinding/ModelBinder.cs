using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace Voyager.ModelBinding;

public enum ModelBindingSource
{
	Route,
	Query,
	Cookie,
	Header,
	Form
}

public class ModelBinder : IModelBinder
{
	private JsonSerializerOptions? jsonOptions;

	private readonly NumberFormatInfo numberFormatter = new();

	public bool GetBool(HttpContext context, ModelBindingSource source, string key, DefaultValue<bool>? defaultValue = null)
	{
		var value = GetStringValues(context, source, key).First();
		if (value != null && bool.TryParse(value, out var result))
		{
			return result;
		}
		return defaultValue != null && defaultValue.Value;
	}

	public bool TryGetBool(HttpContext context, ModelBindingSource source, string key, out bool boolean, DefaultValue<bool>? defaultValue = null)
	{
		var value = GetStringValues(context, source, key).FirstOrDefault();
		if (value != null && bool.TryParse(value, out var result))
		{
			boolean = result;
			return true;
		}
		boolean = defaultValue != null && defaultValue.Value;
		return defaultValue != null;
	}

	public IEnumerable<bool> GetBoolEnumerable(HttpContext context, ModelBindingSource source, string key, DefaultValue<IEnumerable<bool>>? defaultValue = null)
	{
		var values = GetStringValues(context, source, key);
		if (values != StringValues.Empty)
		{
			return ParseBools(values);

		}
		return defaultValue == null ? Enumerable.Empty<bool>() : defaultValue.Value;
	}

	private IEnumerable<bool> ParseBools(StringValues values)
	{
		foreach (var value in values)
		{
			if (bool.TryParse(value, out var boolean))
			{
				yield return boolean;
			}
		}
	}

	public string GetString(HttpContext context, ModelBindingSource source, string key, DefaultValue<string>? defaultValue = null)
	{
		var value = GetStringValues(context, source, key);
		if (value != StringValues.Empty)
		{
			return value.ToString();
		}
		return defaultValue == null ? string.Empty : defaultValue.Value;
	}

	public bool TryGetString(HttpContext context, ModelBindingSource source, string key, out string result, DefaultValue<string>? defaultValue = null)
	{
		var value = GetStringValues(context, source, key);
		if (value != StringValues.Empty)
		{
			result = value.ToString();
			return true;
		}
		result = defaultValue != null ? defaultValue.Value : string.Empty;
		return defaultValue != null;
	}

	public IEnumerable<string> GetStringEnumerable(HttpContext context, ModelBindingSource source, string key, DefaultValue<IEnumerable<string>>? defaultValue = null)
	{
		var values = GetStringValues(context, source, key);
		if (values != StringValues.Empty)
		{
			return values;

		}
		return defaultValue == null ? Enumerable.Empty<string>() : defaultValue.Value;
	}

	public TNumber GetNumber<TNumber>(HttpContext context, ModelBindingSource source, string key, DefaultValue<TNumber>? defaultValue = null)
		where TNumber : INumber<TNumber>
	{
		var value = GetStringValues(context, source, key).FirstOrDefault();
		if (value != null && TNumber.TryParse(value, numberFormatter, out var result))
		{
			return result;
		}
		return defaultValue == null ? TNumber.Zero : defaultValue.Value;
	}

	private IEnumerable<TNumber> ParseNumbers<TNumber>(StringValues values)
		where TNumber : INumber<TNumber>
	{
		foreach (var value in values)
		{
			if (TNumber.TryParse(value, numberFormatter, out var number))
			{
				yield return number;
			}
		}
	}

	public IEnumerable<TNumber> GetNumberEnumerable<TNumber>(HttpContext context, ModelBindingSource source, string key, DefaultValue<IEnumerable<TNumber>>? defaultValue = null)
		where TNumber : INumber<TNumber>
	{
		var values = GetStringValues(context, source, key);
		if (values != StringValues.Empty)
		{
			return ParseNumbers<TNumber>(values);

		}
		return defaultValue == null ? Enumerable.Empty<TNumber>() : defaultValue.Value;

	}

	public bool TryGetNumber<TNumber>(HttpContext context, ModelBindingSource source, string key, out TNumber number, DefaultValue<TNumber>? defaultValue = null)
		where TNumber : INumber<TNumber>
	{
		var value = GetStringValues(context, source, key).FirstOrDefault();
		if (value != null && TNumber.TryParse(value, numberFormatter, out var result))
		{
			number = result;
			return true;
		}
		number = defaultValue == null ? TNumber.Zero : defaultValue.Value;
		return defaultValue != null;
	}

	public TObject? GetObject<TObject>(HttpContext context, ModelBindingSource source, string key, DefaultValue<TObject>? defaultValue = null)
		where TObject : class
	{
		var value = GetStringValues(context, source, key);
		TObject? obj = null;
		if (value != StringValues.Empty)
		{
			obj = JsonSerializer.Deserialize<TObject>(value.ToString(), GetJsonOptions(context));
		}
		return obj ?? defaultValue?.Value;
	}

	private StringValues GetStringValues(HttpContext context, ModelBindingSource source, string key)
	{
		return source switch
		{
			ModelBindingSource.Cookie => context.Request.Cookies.TryGetValue(key, out var value) ? value : null,
			_ => StringValues.Empty
		};
	}

	private JsonSerializerOptions GetJsonOptions(HttpContext httpContext)
	{
		jsonOptions ??= httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
		return jsonOptions;
	}
}