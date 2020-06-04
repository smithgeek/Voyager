using Microsoft.AspNetCore.Mvc;

namespace Voyager.Api
{
	internal class ActionResultUnauthorizedResponseFactory<T> : UnauthorizedResponseFactory<ActionResult<T>>
	{
		public ActionResult<T> GetUnauthorizedResponse()
		{
			return new UnauthorizedResult();
		}
	}
}