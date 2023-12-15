using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace Voyager.ModelBinding;

public interface IModelBinder
{
	bool GetBool(StringValues values, DefaultValue<bool>? defaultValue = null);
	IEnumerable<bool> GetBoolEnumerable(StringValues values, DefaultValue<IEnumerable<bool>>? defaultValue = null);
	bool TryGetBool(StringValues values, out bool boolean, DefaultValue<bool>? defaultValue = null);

	TNumber GetNumber<TNumber>(StringValues values, DefaultValue<TNumber>? defaultValue = null) where TNumber : INumber<TNumber>;
	IEnumerable<TNumber> GetNumberEnumerable<TNumber>(StringValues values, DefaultValue<IEnumerable<TNumber>>? defaultValue = null) where TNumber : INumber<TNumber>;
	bool TryGetNumber<TNumber>(StringValues values, out TNumber number, DefaultValue<TNumber>? defaultValue = null) where TNumber : INumber<TNumber>;

	string GetString(StringValues values, DefaultValue<string>? defaultValue = null);
	IEnumerable<string> GetStringEnumerable(StringValues values, DefaultValue<IEnumerable<string>>? defaultValue = null);
	bool TryGetString(StringValues values, out string result, DefaultValue<string>? defaultValue = null);

	TObject? GetObject<TObject>(StringValues values, JsonSerializerOptions options, DefaultValue<TObject>? defaultValue = null) where TObject : class;
}