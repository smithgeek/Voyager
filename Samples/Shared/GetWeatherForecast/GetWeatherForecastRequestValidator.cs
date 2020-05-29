using FluentValidation;

namespace Shared.GetWeatherForecast
{
	public class GetWeatherForecastRequestValidator : AbstractValidator<GetWeatherForecastRequest>
	{
		public GetWeatherForecastRequestValidator()
		{
			RuleFor(r => r.Days).GreaterThanOrEqualTo(1);
		}
	}
}