using Microsoft.AspNetCore.Mvc;
using Shared.TestEndpoint;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Shared.Configure.Configure2Services(builder.Services);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

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

Shared.Configure.Configure2(app);
app.MapSwagger();
app.UseSwagger();
app.UseSwaggerUI();

TestEndpoint2.Configure(app.MapGet("/test2", ([FromQuery] string abc, HttpContext httpContext, CancellationToken token) =>
{
	return new
	{
		test = "3"
	};
}));
app.MapGet("/test4", ([AsParameters] NewRequest req) =>
{
	return new { test = "3" };
}).WithOpenApi(operation =>
{
	operation.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
	{
		Name = "SomeParam",
		In = Microsoft.OpenApi.Models.ParameterLocation.Query,
		Schema = new Microsoft.OpenApi.Models.OpenApiSchema { }
	});
	return operation;
});

app.Run();
record struct NewRequest(int value1, [FromQuery] List<int> values, string strVal);

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}