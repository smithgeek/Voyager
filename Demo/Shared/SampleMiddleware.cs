using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Voyager.Api;

namespace Shared
{
	public class SampleMiddleware
	{
		private readonly RequestDelegate next;

		public SampleMiddleware(RequestDelegate next)
		{
			this.next = next;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			if (context.Request.Query.ContainsKey("bad"))
			{
				context.Response.Headers.Add("test", "bad");
			}
			else if (context.Request.Query.ContainsKey("reallybad"))
			{
				await context.WriteResultAsync(new BadRequestObjectResult("You shouldn't do that"));
				return;
			}
			else if (context.Request.Query.ContainsKey("exception"))
			{
				throw new Exception("Bad stuff");
			}
			await next(context);
		}
	}
}