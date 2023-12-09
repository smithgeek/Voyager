#nullable disable
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using Voyager;
using Voyager.ModelBinding;

namespace Voyager.Generated2
{
	internal class EndpointMapper : Voyager.IVoyagerMapping
	{
		private class Converter : JsonConverter<ManualRequestBody>
		{
			public override ManualRequestBody Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				return new ManualRequestBody();
			}

			public override void Write(Utf8JsonWriter writer, ManualRequestBody value, JsonSerializerOptions options)
			{
				throw new NotImplementedException();
			}
		}

		public void MapEndpoints(WebApplication app)
		{
			var instRequestValidator = new ManualRequestValidator();
			var modelBinder = new ModelBinder();
			var endpoint = app.Services.GetRequiredService<VoyagerApi.Endpoint>();
			var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
			VoyagerApi.Endpoint.Configure(app.MapPost("/manual/benchmark/ok/{id}", async (HttpContext context) =>
			{
				var request = await JsonSerializer.DeserializeAsync<VoyagerApi.Request>(context.Request.Body, jsonOptions);
				request.Id = modelBinder.GetNumber<int>(context, ModelBindingSource.Route, "id");
				var validationResult = await instRequestValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return Results.ValidationProblem(validationResult.ToDictionary());
				}
				return TypedResults.Ok(endpoint.Post(request));
			}).WithMetadata((new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddParameter("id", Microsoft.OpenApi.Models.ParameterLocation.Path, typeof(int));
				builder.AddBody(typeof(ManualRequestBody));
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(200, typeof(VoyagerApi.Response));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}))()));
		}


		private class ManualRequestBody
		{
			public string firstName { get; set; }
			public string lastName { get; set; }
			public int age { get; set; }
			public System.Collections.Generic.IEnumerable<string> phoneNumbers { get; set; }
		}
		public class ManualRequestValidator : AbstractValidator<VoyagerApi.Request>
		{
			public ManualRequestValidator()
			{
				VoyagerApi.Request.AddValidationRules(this);
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
			services.AddTransient<VoyagerApi.Endpoint>();
			services.AddTransient<IVoyagerMapping, Voyager.Generated2.EndpointMapper>();
		}
	}
}
