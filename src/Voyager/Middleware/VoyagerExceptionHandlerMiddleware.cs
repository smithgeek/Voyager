using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voyager.Middleware
{
	public class VoyagerExceptionHandlerMiddleware
	{
		private readonly IEnumerable<ExceptionHandlerConfigurator> configurators;
		private readonly ExceptionHandler exceptionHandler;
		private readonly RequestDelegate next;

		public VoyagerExceptionHandlerMiddleware(RequestDelegate next, ExceptionHandler exceptionHandler, IEnumerable<ExceptionHandlerConfigurator> configurators)
		{
			this.next = next;
			this.exceptionHandler = exceptionHandler;
			this.configurators = configurators;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await next(context);
			}
			catch (Exception e)
			{
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