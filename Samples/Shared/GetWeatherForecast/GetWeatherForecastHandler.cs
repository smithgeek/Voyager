using Microsoft.AspNetCore.Http;
using System;
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

		public IResult Get(GetWeatherForecastRequest request)
		{
			IResult SomeFunc()
			{
				return TypedResults.Ok("yes");
			}
			if (request.Days < 1)
			{
				throw new ArgumentException($"Days must be greater than 0");
			}
			if (request.Days > 2)
			{
				var result = TypedResults.NotFound();
				return result;
			}
			else if (request.Days > 10)
			{
				return TypedResults.BadRequest<int>(3);
			}
			var rng = new Random();
			return TypedResults.Ok(Enumerable.Range(1, request.Days).Select(index => new GetWeatherForecastResponse
			{
				Date = DateTime.Now.AddDays(index),
				TemperatureC = rng.Next(-20, 55),
				Summary = Summaries[rng.Next(Summaries.Length)],
				City = request.City
			})
			.ToArray().AsEnumerable());
		}
	}
}