using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Voyager.Api;

namespace Voyager.Middleware
{
	public class VoyagerExceptionHandlerMiddleware
	{
		private readonly ExceptionHandler exceptionHandler;
		private readonly ILogger<VoyagerExceptionHandlerMiddleware> logger;
		private readonly RequestDelegate next;

		public VoyagerExceptionHandlerMiddleware(RequestDelegate next, ILogger<VoyagerExceptionHandlerMiddleware> logger, ExceptionHandler exceptionHandler)
		{
			this.next = next;
			this.logger = logger;
			this.exceptionHandler = exceptionHandler;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				logger.LogTrace($"Try {context.Request.GetType().FullName}");
				await next(context);
			}
			catch (Exception e)
			{
				logger.LogTrace("Handler threw an exception");
				var result = exceptionHandler.HandleException(e);
				await context.WriteResultAsync(result);
			}
		}
	}
}