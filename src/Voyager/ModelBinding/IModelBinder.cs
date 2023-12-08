using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Voyager.ModelBinding;

public interface IModelBinder
{
	bool GetBool(HttpContext context, ModelBindingSource source, string key, DefaultValue<bool>? defaultValue = null);
	IEnumerable<bool> GetBoolEnumerable(HttpContext context, ModelBindingSource source, string key, DefaultValue<IEnumerable<bool>>? defaultValue = null);
	TNumber GetNumber<TNumber>(HttpContext context, ModelBindingSource source, string key, DefaultValue<TNumber>? defaultValue = null) where TNumber : INumber<TNumber>;
	IEnumerable<TNumber> GetNumberEnumerable<TNumber>(HttpContext context, ModelBindingSource source, string key, DefaultValue<IEnumerable<TNumber>>? defaultValue = null) where TNumber : INumber<TNumber>;
	TObject? GetObject<TObject>(HttpContext context, ModelBindingSource source, string key, DefaultValue<TObject>? defaultValue = null) where TObject : class;
	string GetString(HttpContext context, ModelBindingSource source, string key, DefaultValue<string>? defaultValue = null);
	bool TryGetBool(HttpContext context, ModelBindingSource source, string key, out bool boolean, DefaultValue<bool>? defaultValue = null);
	bool TryGetString(HttpContext context, ModelBindingSource source, string key, out string result, DefaultValue<string>? defaultValue = null);
}