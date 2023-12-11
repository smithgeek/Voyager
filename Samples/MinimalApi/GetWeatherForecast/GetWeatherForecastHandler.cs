using Voyager;

namespace Shared.GetWeatherForecast
{
	interface IEndpointConfigure
	{
		static abstract void Configure(RouteHandlerBuilder builder);
	}

	[VoyagerEndpoint("v2/WeatherForecast/{city}")]
	public class GetWeatherForecastHandler : IEndpointConfigure
	{
		private static readonly string[] Summaries =
		[
			"Freezing",
			"Bracing",
			"Chilly",
			"Cool",
			"Mild",
			"Warm",
			"Balmy",
			"Hot",
			"Sweltering",
			"Scorching"
		];

		public static void Configure(RouteHandlerBuilder builder)
		{
			builder.CacheOutput();
		}

		public IResult Get(GetWeatherForecastRequest request)
		{
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
				return request.Days > 20 ? TypedResults.BadRequest(3) : TypedResults.BadRequest("nope");
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