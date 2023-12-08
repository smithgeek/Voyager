using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Voyager.ModelBinding;

public interface IModelBinder
{
	ValueTask<TValue?> GetBody<TValue>();
	ValueTask<TValue?> GetCookieValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null);
	ValueTask<IEnumerable<TValue>> GetCookieValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null);
	ValueTask<TValue?> GetFormValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null);
	ValueTask<IEnumerable<TValue>> GetFormValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null);
	ValueTask<TValue?> GetHeaderValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null);
	ValueTask<IEnumerable<TValue>> GetHeaderValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null);
	ValueTask<TValue?> GetQueryStringValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null);
	ValueTask<IEnumerable<TValue>> GetQueryStringValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null);
	ValueTask<TValue?> GetRouteValue<TValue>(string key, DefaultValue<TValue>? defaultValue = null);
	ValueTask<IEnumerable<TValue>> GetRouteValues<TValue>(string key, DefaultValue<IEnumerable<TValue>>? defaultValue = null);
}

public interface IModelBinderSingleton
{
	TNumber GetNumber<TNumber>(HttpContext context, ModelBindingSource source, string key, DefaultValue<TNumber>? defaultValue = null) where TNumber : INumber<TNumber>;
	TValue? GetRouteValue<TValue>(HttpContext httpContext, string key, DefaultValue<TValue>? defaultValue = null);
	int GetRouteValueInt(HttpContext httpContext, string key, DefaultValue<int>? defaultValue = null);
}