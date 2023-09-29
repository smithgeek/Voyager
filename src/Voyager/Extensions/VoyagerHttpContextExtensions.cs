using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Voyager
{
	public static class VoyagerHttpContextExtensions
	{
		private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

		private static readonly RouteData EmptyRouteData = new RouteData();

		public static string? GetUserIpAddress(this HttpContext context)
		{
			if (context.Request.Headers.ContainsKey("X-Real-IP"))
			{
				return context.Request.Headers["X-Real-IP"];
			}
			if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
			{
				return context.Request.Headers["X-Forwarded-For"];
			}
			return context.Connection.RemoteIpAddress?.ToString();
		}

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
				var actionResultMapper = context.RequestServices.GetRequiredService<IActionResultTypeMapper>();
				var resultDataType = actionResultMapper.GetResultDataType(response.GetType());
				actionResult = actionResultMapper.Convert(response, resultDataType);
			}
			return actionResult.ExecuteResultAsync(context.CreateActionContext());
		}

		internal static ActionContext CreateActionContext(this HttpContext context)
		{
			return new ActionContext(context, context.GetRouteData() ?? EmptyRouteData, EmptyActionDescriptor);
		}
	}
}