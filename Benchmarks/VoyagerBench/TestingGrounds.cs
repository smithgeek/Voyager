#nullable disable
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using Voyager;
using Voyager.ModelBinding;
using VoyagerApi;

namespace Voyager.Generated2
{
	internal class EndpointMapper : Voyager.IVoyagerMapping
	{
		public void MapEndpoints(WebApplication app)
		{
			var modelBinder = new ModelBinder();
			var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
			var instRequestValidator = new ManualRequestValidator();
			var endpoint = app.Services.GetRequiredService<VoyagerApi.Endpoint>();
			VoyagerApi.Endpoint.Configure(app.MapPost("/manual/benchmark/ok/{id}", async ([Microsoft.AspNetCore.Mvc.FromRoute] int id, HttpContext context) =>
			{
				var request = await JsonSerializer.DeserializeAsync<VoyagerApi.Request>(context.Request.Body, jsonOptions);
				//var body = await JsonSerializer.DeserializeAsync<ManualRequestBody>(context.Request.Body, jsonOptions);
				//var request = new VoyagerApi.Request
				//{
				//	Id = id,// modelBinder.GetNumber<int>(context, ModelBindingSource.Route, "id"),
				//	Age = body.age,
				//	FirstName = body.firstName,
				//	LastName = body.lastName,
				//	PhoneNumbers = body.phoneNumbers
				//};
				var validationResult = await instRequestValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return Results.ValidationProblem(validationResult.ToDictionary());
				}
				return TypedResults.Ok(endpoint.Post(request));
			}).WithMetadata((new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddParameter("id", Microsoft.OpenApi.Models.ParameterLocation.Path, typeof(int), true);
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
				VoyagerApi.Request.Validate(this);
			}
		}

	}
}

namespace VoyagerApi
{
	public partial class Request
	{
		public Request() { }
		public Request(ref Utf8JsonReader reader, HttpContext httpContext, JsonSerializerOptions options)
		{
			Id = (int)httpContext.Items["req_id"];
			while (reader.TokenType != JsonTokenType.EndObject)
			{
				if (reader.TokenType == JsonTokenType.PropertyName)
				{
					var propName = reader.GetString();
					switch (propName)
					{
						case "age":
							reader.Read();
							Age = reader.GetInt32();
							break;

						case "firstName":
							reader.Read();
							FirstName = reader.GetString();
							break;

						case "lastName":
							reader.Read();
							LastName = reader.GetString();
							break;

						case "phoneNumbers":
							reader.Read();
							PhoneNumbers = JsonSerializer.Deserialize<IEnumerable<string>>(ref reader, options);
							break;
					}
				}
				reader.Read();
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
