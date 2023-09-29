using Microsoft.AspNetCore.Http;

namespace Voyager
{
	public interface IInjectHttpContext
	{
		HttpContext? HttpContext { get; set; }
	}
}