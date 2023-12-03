using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Shared.Configure.Configure2Services(builder.Services);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
	opt.AddVoyager();
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.

var summaries = new[]
{
	"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

//app.MapGet("/weatherforecast", () =>
//{
//	var forecast = Enumerable.Range(1, 5).Select(index =>
//		new WeatherForecast
//		(
//			DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//			Random.Shared.Next(-20, 55),
//			summaries[Random.Shared.Next(summaries.Length)]
//		))
//		.ToArray();
//	return forecast;
//});

app.MapSwagger();
app.UseSwagger();
app.UseSwaggerUI();

//TestEndpoint2.Configure(app.MapGet("/test2", ([FromQuery] string abc, HttpContext httpContext, CancellationToken token) =>
//{
//	return new
//	{
//		test = "3"
//	};
//}));
app.MapGet("/test4", (NewRequest req) =>
{
	return new { test = "3" };
}).WithOpenApi(op =>
{
	op.Parameters.Add(new OpenApiParameter
	{
		Name = "SomethingNew",
		In = ParameterLocation.Query,
		Schema = OpenApiGenerator.GenerateSchema(app.Services, typeof(int)),
	});
	return op;
});

app.Run();

public static class OpenApiGenerator
{
	private static ISchemaGenerator? generator;

	public static OpenApiSchema GenerateSchema(IServiceProvider services, Type type)
	{
		return (generator ?? CreateSchemaGenerator(services)).GenerateSchema(type, new());
	}

	private static ISchemaGenerator CreateSchemaGenerator(IServiceProvider services)
	{
		var gen = services.GetService<ISchemaGenerator>()
			?? new SchemaGenerator(
				services.GetService<SchemaGeneratorOptions>() ?? new(),
				services.GetService<ISerializerDataContractResolver>() ??
					new JsonSerializerDataContractResolver(
						services.GetService<IOptions<JsonOptions>>()?.Value?.JsonSerializerOptions
						?? new JsonSerializerOptions()));
		generator = gen;
		return gen;
	}
}

public record struct NewRequest(int value1, [FromQuery] List<int> values, string strVal)
{
	public static ValueTask<NewRequest?> BindAsync(HttpContext context, ParameterInfo parameter)
	{
		return ValueTask.FromResult<NewRequest?>(null);
	}
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}