using Microsoft.AspNetCore.Mvc;
using Shared.GetWeatherForecast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class WeatherForecastController : ControllerBase
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		[HttpGet]
		[Route("{city}")]
		public IEnumerable<GetWeatherForecastResponse> Get([FromRoute] string city, [FromQuery(Name = "d")] int days = 5)
		{
			var rng = new Random();
			return Enumerable.Range(1, days).Select(index => new GetWeatherForecastResponse
			{
				City = city,
				Date = DateTime.Now.AddDays(index),
				TemperatureC = rng.Next(-20, 55),
				Summary = Summaries[rng.Next(Summaries.Length)]
			})
			.ToArray();
		}
	}
}