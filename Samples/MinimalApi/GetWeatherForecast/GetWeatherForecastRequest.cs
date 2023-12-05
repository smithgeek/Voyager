using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Shared.GetWeatherForecast
{
	public class GetWeatherForecastRequest
	{
		[FromRoute]
		public required string City { get; set; }

		[FromQuery(Name = "d")]
		public int Days { get; set; } = 5;

		[JsonPropertyName("test1")]
		public required string Test { get; set; }

		public static void AddValidationRules(AbstractValidator<GetWeatherForecastRequest> validator)
		{
			validator.RuleFor(r => r.Days).GreaterThanOrEqualTo(1);
		}
	}
}