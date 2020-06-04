using Microsoft.AspNetCore.Mvc;

namespace Voyager.Api
{
	internal class IActionResultUnauthorizedResponseFactory<TResponse> : UnauthorizedResponseFactory<IActionResult>
		where TResponse : IActionResult
	{
		public IActionResult GetUnauthorizedResponse()
		{
			return new UnauthorizedResult();
		}
	}
}