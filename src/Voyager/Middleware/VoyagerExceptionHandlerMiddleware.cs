using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voyager.Middleware
{
	public class VoyagerExceptionHandlerMiddleware
	{
		private readonly IEnumerable<ExceptionHandlerConfigurator> configurators;
		private readonly ExceptionHandler exceptionHandler;
		private readonly ILogger<VoyagerExceptionHandlerMiddleware> logger;
		private readonly RequestDelegate next;

		public VoyagerExceptionHandlerMiddleware(RequestDelegate next,
			ExceptionHandler exceptionHandler, IEnumerable<ExceptionHandlerConfigurator> configurators, ILogger<VoyagerExceptionHandlerMiddleware> logger)
		{
			this.next = next;
			this.exceptionHandler = exceptionHandler;
			this.configurators = configurators;
			this.logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await next(context);
			}
			catch (Exception e)
			{
				logger.LogError(e, e.Message);
				foreach (var configurator in configurators)
				{
					configurator.Configure();
				}
				var result = exceptionHandler.HandleException(e);
				await context.WriteResultAsync(result);
			}
		}
	}
}