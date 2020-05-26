using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
	public static class VoyagerHttpContextExtensions
	{
		public static string GetUserIpAddress(this HttpContext context)
		{
			if (context.Request.Headers.ContainsKey("X-Real-IP"))
			{
				return context.Request.Headers["X-Real-IP"];
			}
			if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
			{
				return context.Request.Headers["X-Forwarded-For"];
			}
			return context.Connection.RemoteIpAddress.ToString();
		}

		private static readonly RouteData EmptyRouteData = new RouteData();

		private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

		public static Task WriteResultAsync(this HttpContext context, object response)
		{
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}

			IActionResult actionResult;
			if (response is IActionResult responseAction)
			{
				actionResult = responseAction;
			}
			else
			{
				var actionResultMapper = context.RequestServices.GetService<IActionResultTypeMapper>();
				var resultDataType = actionResultMapper.GetResultDataType(response.GetType());
				actionResult = actionResultMapper.Convert(response, resultDataType);
			}
			return actionResult.ExecuteResultAsync(new ActionContext(context, context.GetRouteData() ?? EmptyRouteData, EmptyActionDescriptor));
		}
	}
}