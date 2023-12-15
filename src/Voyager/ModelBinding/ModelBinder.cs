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
	private readonly NumberFormatInfo numberFormatter = new();

	public bool GetBool(StringValues values, DefaultValue<bool>? defaultValue = null)
	{
		var value = values.FirstOrDefault();
		if (value != null && bool.TryParse(value, out var result))
		{
			return result;
		}
		return defaultValue != null && defaultValue.Value;
	}

	public bool TryGetBool(StringValues values, out bool boolean, DefaultValue<bool>? defaultValue = null)
	{
		var value = values.FirstOrDefault();
		if (value != null && bool.TryParse(value, out var result))
		{
			boolean = result;
			return true;
		}
		boolean = defaultValue != null && defaultValue.Value;
		return defaultValue != null;
	}

	public IEnumerable<bool> GetBoolEnumerable(StringValues values, DefaultValue<IEnumerable<bool>>? defaultValue = null)
	{
		if (values != StringValues.Empty)
		{
			return ParseBools(values);

		}
		return defaultValue == null ? Enumerable.Empty<bool>() : defaultValue.Value;
	}

	private static IEnumerable<bool> ParseBools(StringValues values)
	{
		foreach (var value in values)
		{
			if (bool.TryParse(value, out var boolean))
			{
				yield return boolean;
			}
		}
	}

	public string GetString(StringValues values, DefaultValue<string>? defaultValue = null)
	{
		var value = values;
		if (value != StringValues.Empty)
		{
			return value.ToString();
		}
		return defaultValue == null ? string.Empty : defaultValue.Value;
	}

	public bool TryGetString(StringValues values, out string result, DefaultValue<string>? defaultValue = null)
	{
		var value = values;
		if (value != StringValues.Empty)
		{
			result = value.ToString();
			return true;
		}
		result = defaultValue != null ? defaultValue.Value : string.Empty;
		return defaultValue != null;
	}

	public IEnumerable<string> GetStringEnumerable(StringValues values, DefaultValue<IEnumerable<string>>? defaultValue = null)
	{
		if (values != StringValues.Empty)
		{
			return values;

		}
		return defaultValue == null ? Enumerable.Empty<string>() : defaultValue.Value;
	}

	public TNumber GetNumber<TNumber>(StringValues values, DefaultValue<TNumber>? defaultValue = null)
		where TNumber : INumber<TNumber>
	{
		var value = values.FirstOrDefault();
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

	public IEnumerable<TNumber> GetNumberEnumerable<TNumber>(StringValues values, DefaultValue<IEnumerable<TNumber>>? defaultValue = null)
		where TNumber : INumber<TNumber>
	{
		if (values != StringValues.Empty)
		{
			return ParseNumbers<TNumber>(values);

		}
		return defaultValue == null ? Enumerable.Empty<TNumber>() : defaultValue.Value;

	}

	public bool TryGetNumber<TNumber>(StringValues values, out TNumber number, DefaultValue<TNumber>? defaultValue = null)
		where TNumber : INumber<TNumber>
	{
		var value = values.FirstOrDefault();
		if (value != null && TNumber.TryParse(value, numberFormatter, out var result))
		{
			number = result;
			return true;
		}
		number = defaultValue == null ? TNumber.Zero : defaultValue.Value;
		return defaultValue != null;
	}

	public TObject? GetObject<TObject>(StringValues values, JsonSerializerOptions options, DefaultValue<TObject>? defaultValue = null)
		where TObject : class
	{
		var value = values;
		TObject? obj = null;
		if (value != StringValues.Empty)
		{
			try
			{
				obj = JsonSerializer.Deserialize<TObject>(value.ToString(), options);
			}
			catch (JsonException)
			{
			}
		}
		return obj ?? defaultValue?.Value;
	}
}
