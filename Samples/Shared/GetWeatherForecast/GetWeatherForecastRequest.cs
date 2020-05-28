using System.Collections.Generic;
using Voyager.Api;

namespace Shared.GetWeatherForecast
{
	[Route(HttpMethod.Get, "v2/WeatherForecast/{city}")]
	public class GetWeatherForecastRequest : EndpointRequest<IEnumerable<GetWeatherForecastResponse>>
	{
		[FromRoute]
		public string City { get; set; }

		[FromQuery("d")]
		public int Days { get; set; } = 5;
	}
}