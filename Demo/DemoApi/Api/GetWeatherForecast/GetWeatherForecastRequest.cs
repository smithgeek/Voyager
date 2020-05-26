using System.Collections.Generic;
using Voyager.Api;

namespace DemoApi.Api.GetWeatherForecast
{
	[Route(HttpMethod.Get, "v2/WeatherForecast/{city}")]
	public class GetWeatherForecastRequest : EndpointRequest<IEnumerable<GetWeatherForecastResponse>>
	{
		[FromQuery("d")]
		public int Days { get; set; } = 5;

		[FromRoute]
		public string City { get; set; }
	}
}