using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Voyager.Api
{
	public interface ModelBinder
	{
		Task<object> Bind(HttpContext context, Type objectType);

		Task<TRequest> Bind<TRequest>(HttpContext context);

		Task<TRequest> Bind<TRequest, TResponse>(HttpContext context);
	}
}