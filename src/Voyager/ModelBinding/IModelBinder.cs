using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voyager.ModelBinding
{
	public interface IModelBinder
	{
		ValueTask<TValue?> GetBody<TValue>();
		ValueTask<TValue?> GetBodyValue<TValue>(string key);
		ValueTask<TValue?> GetFormValue<TValue>(string key);
		ValueTask<IEnumerable<TValue?>> GetFormValues<TValue>(string key);
		ValueTask<TValue?> GetQueryStringValue<TValue>(string key);
		ValueTask<IEnumerable<TValue?>> GetQueryStringValues<TValue>(string key);
		ValueTask<TValue?> GetRouteValue<TValue>(string key);
		ValueTask<IEnumerable<TValue?>> GetRouteValues<TValue>(string key);
	}
}