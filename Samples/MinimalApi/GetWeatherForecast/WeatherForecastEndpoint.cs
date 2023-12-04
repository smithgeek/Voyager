using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Voyager;

namespace MinimalApi.GetWeatherForecast;

[VoyagerEndpoint("/weatherforecast/{city}")]
public class WeatherForecastEndpoint
{
	private readonly string[] summaries = ["Freezing",
		"Bracing",
		"Chilly",
		"Cool",
		"Mild",
		"Warm",
		"Balmy",
		"Hot",
		"Sweltering",
		"Scorching"];

	public WeatherForecast[] Get(WeatherForecastRequest request)
	{
		var forecast = Enumerable.Range(1, request.Days).Select(index =>
		new WeatherForecast
		(
			request.City,
			DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
			Random.Shared.Next(-20, 55),
			summaries[Random.Shared.Next(summaries.Length)]
		)).ToArray();
		return forecast;
	}
}

public class WeatherForecastRequest
{
	[FromRoute]
	public required string City { get; set; }

	[FromQuery(Name = "d")]
	public int Days { get; set; } = 5;

	public static void AddValidationRules(AbstractValidator<WeatherForecastRequest> validator)
	{
		validator.RuleFor(r => r.Days).InclusiveBetween(1, 5);
	}
}

public record WeatherForecast(string City, DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}