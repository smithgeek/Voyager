using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using Voyager.Api;
using Voyager.Api.Authorization;

namespace DemoFunctionsApp.TypedActionResultException
{
	public class TypedActionResultExceptionHandler : EndpointHandler<TypedActionResultExceptionRequest, bool, AnonymousPolicy>
	{
		public TypedActionResultExceptionHandler(IHttpContextAccessor httpContextAccessor)
			: base(httpContextAccessor)
		{
		}

		public override ActionResult<bool> HandleRequest(TypedActionResultExceptionRequest request)
		{
			throw new Exception("Exception!");
		}
	}
}