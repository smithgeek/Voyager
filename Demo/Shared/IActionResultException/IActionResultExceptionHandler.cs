using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using Voyager.Api;
using Voyager.Api.Authorization;

namespace DemoFunctionsApp.IActionResultException
{
	public class IActionResultExceptionHandler : EndpointHandler<IActionResultExceptionRequest, AnonymousPolicy>
	{
		public IActionResultExceptionHandler(IHttpContextAccessor httpContextAccessor)
			: base(httpContextAccessor)
		{
		}

		public override IActionResult HandleRequest(IActionResultExceptionRequest request)
		{
			throw new Exception("Exception Happened!");
		}
	}
}