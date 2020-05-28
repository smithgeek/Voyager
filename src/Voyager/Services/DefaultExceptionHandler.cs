using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using Voyager.Api;
using Voyager.Configuration;

namespace Voyager.Middleware
{
	public class DefaultExceptionHandler : ExceptionHandler
	{
		private readonly VoyagerConfiguration configuration;
		private readonly ILogger<DefaultExceptionHandler> logger;

		public DefaultExceptionHandler(VoyagerConfiguration configuration, ILogger<DefaultExceptionHandler> logger)
		{
			this.configuration = configuration;
			this.logger = logger;
		}

		public IActionResult HandleException<TException>(TException exception) where TException : Exception
		{
			if (exception is ValidationException validationException)
			{
				logger.LogInformation(exception, "Validation error.");
				return new BadRequestObjectResult(
					new ValidationProblemDetails(
						new Dictionary<string, string[]>
						{
							{ "validationErrors", validationException.Errors.Select(error => error.ErrorMessage).ToArray() }
						}
					)
					{
						Status = (int)HttpStatusCode.BadRequest,
						Title = "Validation error",
						Detail = validationException.Errors.First().ErrorMessage,
					}
				);
			}
			else if (exception is ProblemDetailsException problemDetailsException)
			{
				if (configuration.IsDevelopment())
				{
					problemDetailsException.Problem.Extensions["StackTrace"] = problemDetailsException.StackTrace;
				}
				return new BadRequestObjectResult(problemDetailsException.Problem);
			}
			else
			{
				logger.LogError(exception, exception.Message);
				var problem = new ProblemDetails
				{
					Status = (int)HttpStatusCode.InternalServerError,
					Title = configuration.IsDevelopment() ? exception.GetType().Name : "Internal Server Error",
					Detail = configuration.IsDevelopment() ? exception.Message : "Internal Server Error",
				};
				problem.Extensions["stackTrace"] = configuration.IsDevelopment() ? exception.StackTrace : null;
				return new ContentResult
				{
					StatusCode = (int)HttpStatusCode.InternalServerError,
					Content = JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
					ContentType = "application/json"
				};
			}
		}
	}
}