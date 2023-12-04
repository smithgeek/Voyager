using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using Voyager;

namespace Shared.GetWeatherForecast
{
	[VoyagerEndpoint("v2/WeatherForecast/{city}")]
	public class GetWeatherForecastHandler
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		public IEnumerable<GetWeatherForecastResponse> Get(GetWeatherForecastRequest request, ValidationResult validationResults)
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