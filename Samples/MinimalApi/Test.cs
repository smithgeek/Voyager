using FluentValidation;
using Voyager.ModelBinding;

namespace Voyager;

public static class VoyagerEndpoints
{
	public static void AddVoyagerServices(IServiceCollection services)
	{
		AddVoyager(services);
	}

	public static void MapVoyagerEndpoints(WebApplication app)
	{
		MapVoyager(app);
	}

	internal static void AddVoyager(this IServiceCollection services)
	{
		services.AddTransient<Shared.TestEndpoint.TestEndpointHandler>();
		services.AddTransient<Shared.TestEndpoint.TestEndpoint2>();
		services.AddTransient<Shared.GetWeatherForecast.GetWeatherForecastHandler>();
	}

	internal static void MapVoyager(this WebApplication app)
	{
		app.MapPost("/test", async (HttpContext context) =>
		{
			var endpoint = context.RequestServices.GetRequiredService<Shared.TestEndpoint.TestEndpointHandler>();
			endpoint.HttpContext = context;
			var modelBinder = context.RequestServices.GetService<IModelBinder>() ?? new ModelBinder(context);
			var body = await modelBinder.GetBody<TestEndpointRequestBody>();
			var request = new Shared.TestEndpoint.TestEndpointRequest
			{
				Item1 = body.Item1,
				Item2 = body.Item2,
				List = await modelBinder.GetQueryStringValues<int>("List"),
				MyCookie = await modelBinder.GetCookieValue<string?>("MyCookie"),
				MyHeader = await modelBinder.GetHeaderValue<string?>("MyHeader"),
				Other = await modelBinder.GetQueryStringValue<string>("Other"),
			};
			var validator = new TestEndpointRequestValidator();
			var validationResult = await validator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return Results.ValidationProblem(validationResult.ToDictionary());
			}
			return TypedResults.Ok(endpoint.Post(request));
		}).WithOpenApi(operation =>
		{
			var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, operation);
			builder.AddParameter("List", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(System.Collections.Generic.IEnumerable<int>));
			builder.AddParameter("MyCookie", Microsoft.OpenApi.Models.ParameterLocation.Cookie, typeof(string));
			builder.AddParameter("MyHeader", Microsoft.OpenApi.Models.ParameterLocation.Header, typeof(string));
			builder.AddParameter("Other", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(string));
			builder.AddBody(typeof(TestEndpointRequestBody));
			builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
			builder.AddResponse(200, typeof(Shared.TestEndpoint.TestEndpointResponse));
			return builder.Build();
		});
		Shared.TestEndpoint.TestEndpoint2.Configure(app.MapPost("/test2", async (HttpContext context) =>
		{
			var endpoint = context.RequestServices.GetRequiredService<Shared.TestEndpoint.TestEndpoint2>();
			endpoint.CancellationToken = context.RequestAborted;
			endpoint.HttpContext = context;
			var modelBinder = context.RequestServices.GetService<IModelBinder>() ?? new ModelBinder(context);
			var body = await modelBinder.GetBody<TestEndpointRequestBody>();
			var request = new Shared.TestEndpoint.TestEndpointRequest
			{
				Item1 = body.Item1,
				Item2 = body.Item2,
				List = await modelBinder.GetQueryStringValues<int>("List"),
				MyCookie = await modelBinder.GetCookieValue<string?>("MyCookie"),
				MyHeader = await modelBinder.GetHeaderValue<string?>("MyHeader"),
				Other = await modelBinder.GetQueryStringValue<string>("Other"),
			};
			var validator = new TestEndpointRequestValidator();
			var validationResult = await validator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return Results.ValidationProblem(validationResult.ToDictionary());
			}
			return TypedResults.Ok(endpoint.Post(request, context, context.RequestAborted));
		}).WithOpenApi(operation =>
		{
			var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, operation);
			builder.AddParameter("List", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(System.Collections.Generic.IEnumerable<int>));
			builder.AddParameter("MyCookie", Microsoft.OpenApi.Models.ParameterLocation.Cookie, typeof(string));
			builder.AddParameter("MyHeader", Microsoft.OpenApi.Models.ParameterLocation.Header, typeof(string));
			builder.AddParameter("Other", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(string));
			builder.AddBody(typeof(TestEndpointRequestBody));
			builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
			builder.AddResponse(200, typeof(Shared.TestEndpoint.TestEndpointResponse));
			return builder.Build();
		}));
		app.MapGet("v2/WeatherForecast/{city}", async (HttpContext context) =>
		{
			var endpoint = context.RequestServices.GetRequiredService<Shared.GetWeatherForecast.GetWeatherForecastHandler>();
			var modelBinder = context.RequestServices.GetService<IModelBinder>() ?? new ModelBinder(context);
			var request = new Shared.GetWeatherForecast.GetWeatherForecastRequest
			{
				City = await modelBinder.GetRouteValue<string>("City"),
				Days = await modelBinder.GetQueryStringValue<int>("Days"),
				Test = await modelBinder.GetQueryStringValue<string>("Test"),
			};
			var validator = new GetWeatherForecastRequestValidator();
			Shared.GetWeatherForecast.GetWeatherForecastRequest.AddValidationRules(validator);
			var validationResult = await validator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return Results.ValidationProblem(validationResult.ToDictionary());
			}
			return (IResult)(endpoint.Get(request));
		}).WithOpenApi(operation =>
		{
			var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, operation);
			builder.AddParameter("City", Microsoft.OpenApi.Models.ParameterLocation.Path, typeof(string));
			builder.AddParameter("Days", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(int));
			builder.AddParameter("Test", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(string));
			builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
			builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.NotFound().StatusCode, null);
			builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.BadRequest().StatusCode, typeof(int));
			builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.Ok().StatusCode, typeof(System.Collections.Generic.IEnumerable<Shared.GetWeatherForecast.GetWeatherForecastResponse>));
			return builder.Build();
		});
	}

	public class GetWeatherForecastRequestValidator : AbstractValidator<Shared.GetWeatherForecast.GetWeatherForecastRequest>
	{
		public GetWeatherForecastRequestValidator()
		{
			RuleFor(r => r.City).NotNull();
			RuleFor(r => r.Test).NotNull();
		}
	}

	public class TestEndpointRequestValidator : AbstractValidator<Shared.TestEndpoint.TestEndpointRequest>
	{
		public TestEndpointRequestValidator()
		{
			RuleFor(r => r.Item1).NotNull();
			RuleFor(r => r.Item2).NotNull();
			RuleFor(r => r.List).NotNull();
			RuleFor(r => r.Other).NotNull();
		}
	}

	private class TestEndpointRequestBody
	{
		public string Item1 { get; set; }
		public int Item2 { get; set; }
	}
}