#nullable enable
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Voyager;
using Voyager.ModelBinding;

namespace Voyager.Generated.VoyagerBenchGen2
{
	internal class EndpointMapper : Voyager.IVoyagerMapping
	{
		public void MapEndpoints(WebApplication app)
		{
			var modelBinder = app.Services.GetService<IModelBinder>() ?? new ModelBinder();
			var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
			var inst_VoyagerApi_NoRequestEndpoint = app.Services.GetRequiredService<VoyagerApi.NoRequestEndpoint>();
			var instRequestValidator = new RequestValidator();
			var inst_VoyagerApi_Endpoint = app.Services.GetRequiredService<VoyagerApi.Endpoint>();
			app.MapGet("/norequest", (HttpContext context) =>
			{
				return TypedResults.Ok(inst_VoyagerApi_NoRequestEndpoint.Get());
			}
			).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(200, typeof(int));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}
			)());
			VoyagerApi.Endpoint.Configure(app.MapPost("/benchmark/ok/{id}", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromRouteAttribute(Name = "id")] int id, string? firstName, string? lastName, int age, System.Collections.Generic.IEnumerable<string>? phoneNumbers) =>
			{
				var body = await JsonSerializer.DeserializeAsync<RequestBody>(context.Request.Body, jsonOptions);
				var request = new VoyagerApi.Request
				{
					Id = id,
					FirstName = body?.FirstName ?? default,
					LastName = body?.LastName ?? default,
					Age = body?.Age ?? default,
					PhoneNumbers = body?.PhoneNumbers ?? default,
				};
				var validationResult = await instRequestValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return Results.ValidationProblem(validationResult.ToDictionary());
				}
				return TypedResults.Ok(inst_VoyagerApi_Endpoint.Post(request, context.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<VoyagerApi.Program>>()));
			}
			).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddParameter("id", Microsoft.OpenApi.Models.ParameterLocation.Path, typeof(int), false);
				builder.AddBody(typeof(RequestBody));
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(200, typeof(VoyagerApi.Response));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}
			)()));
		}

		private class RequestBody
		{
			public string? FirstName { get; set; }
			public string? LastName { get; set; }
			public int Age { get; set; }
			public System.Collections.Generic.IEnumerable<string>? PhoneNumbers { get; set; }
		}
		public class RequestValidator : AbstractValidator<VoyagerApi.Request>
		{
			public RequestValidator()
			{
				VoyagerApi.Request.Validate(this);
			}
		}
	}
}

namespace Microsoft.Extensions.DependencyInjection
{
	internal static class VoyagerEndpoints2
	{
		internal static void AddVoyager2(this IServiceCollection services)
		{
			services.AddTransient<VoyagerApi.NoRequestEndpoint>();
			services.AddTransient<VoyagerApi.Endpoint>();
			services.AddTransient<IVoyagerMapping, Voyager.Generated.VoyagerBenchGen2.EndpointMapper>();
		}
	}
}
