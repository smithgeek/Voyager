using FluentValidation;
using System.Collections.Generic;
using Voyager.Api;
using Voyager.Api.Authorization;

namespace Shared.GetWeatherForecast
{
	[VoyagerRoute(HttpMethod.Get, "v2/WeatherForecast/{city}")]
	public class GetWeatherForecastRequest : EndpointRequest<IEnumerable<GetWeatherForecastResponse>>, Enforce<AuthenticatedPolicy>
	{
		[FromRoute]
		public string City { get; set; }

		[FromQuery("d")]
		public int Days { get; set; } = 5;

		public static void AddValidationRules(AbstractValidator<GetWeatherForecastRequest> validator)
		{
			validator.RuleFor(r => r.Days).GreaterThanOrEqualTo(1);
		}
	}
}