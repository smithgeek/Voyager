using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Voyager.ModelBinding
{
	public interface IStringValuesProvider
	{
		StringValues GetStringValues(HttpContext context, ModelBindingSource source, string key);
	}
}