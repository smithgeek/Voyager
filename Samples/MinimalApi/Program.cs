using Microsoft.AspNetCore.Http.HttpResults;
using Voyager.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

var summaries = new[]
{
	"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
	var forecast = Enumerable.Range(1, 5).Select(index =>
		new WeatherForecast
		(
			DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
			Random.Shared.Next(-20, 55),
			summaries[Random.Shared.Next(summaries.Length)]
		))
		.ToArray();
	return forecast;
});

app.MapGet("", (context) =>
{
	return new SomeHandler().Handle(new SomeRequest());
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class SomeRequest
{

}

[VoyagerEndpoint(Voyager.Api.HttpMethod.Get, "/test")]
public class SomeHandler
{
	public async Task<Results<BadRequest, Ok<int>>> Handle(SomeRequest request)
	{
		await Task.Delay(100);
		return TypedResults.Ok(3);
	}
}
