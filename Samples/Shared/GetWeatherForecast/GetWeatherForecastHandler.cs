using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Voyager.Api;
using Voyager.Api.Authorization;

namespace Shared.GetWeatherForecast
{
	public class GetWeatherForecastHandler : EndpointHandler<GetWeatherForecastRequest, IEnumerable<GetWeatherForecastResponse>, AnonymousPolicy>
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		public GetWeatherForecastHandler(IHttpContextAccessor httpContextAccessor)
			: base(httpContextAccessor)
		{
		}

		public override ActionResult<IEnumerable<GetWeatherForecastResponse>> HandleRequest(GetWeatherForecastRequest request)
		{
			if (request.Days < 1)
			{
				throw new ArgumentException("Days must be greater than 0");
			}
			var rng = new Random();
			return Enumerable.Range(1, request.Days).Select(index => new GetWeatherForecastResponse
			{
				Date = DateTime.Now.AddDays(index),
				TemperatureC = rng.Next(-20, 55),
				Summary = Summaries[rng.Next(Summaries.Length)],
				City = request.City
			})
			.ToArray();
		}
	}
}