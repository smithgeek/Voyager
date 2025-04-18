using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Shared.GetWeatherForecast
{
	public class GetWeatherForecastRequest
	{
		[FromRoute]
		public required string City { get; set; }

		[FromQuery(Name = "d")]
		public int Days { get; set; } = 5;
	}

	public class GetWeatherForecastRequestValidator : AbstractValidator<GetWeatherForecastRequest>
	{
		public GetWeatherForecastRequestValidator()
		{
			RuleFor(r => r.Days).GreaterThanOrEqualTo(1);
		}
	}
}