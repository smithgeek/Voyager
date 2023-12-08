#nullable disable
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Voyager;
using Voyager.Generated2;
using Voyager.ModelBinding;

namespace Voyager.Generated2
{
	internal class EndpointMapper : Voyager.IVoyagerMapping
	{
		public static JsonPropertyInfo[] CreateTypeInfoProperties(JsonSerializerOptions options)
		{
			var properties = new global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo[4];

			var info1 = new global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(global::VoyagerApi.Request),
				Converter = null,
				Getter = static obj => ((global::VoyagerApi.Request)obj).FirstName,
				Setter = static (obj, value) => ((global::VoyagerApi.Request)obj).FirstName = value!,
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "FirstName",
				JsonPropertyName = "firstName"
			};

			properties[0] = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreatePropertyInfo<string>(options, info1);

			var info2 = new global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<string>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(global::VoyagerApi.Request),
				Converter = null,
				Getter = static obj => ((global::VoyagerApi.Request)obj).LastName,
				Setter = static (obj, value) => ((global::VoyagerApi.Request)obj).LastName = value!,
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "LastName",
				JsonPropertyName = "lastName"
			};

			properties[1] = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreatePropertyInfo<string>(options, info2);

			var info3 = new global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<int>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(global::VoyagerApi.Request),
				Converter = null,
				Getter = static obj => ((global::VoyagerApi.Request)obj).Age,
				Setter = static (obj, value) => ((global::VoyagerApi.Request)obj).Age = value!,
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "Age",
				JsonPropertyName = "age"
			};

			properties[2] = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreatePropertyInfo<int>(options, info3);

			var info4 = new global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<global::System.Collections.Generic.IEnumerable<string>>
			{
				IsProperty = true,
				IsPublic = true,
				IsVirtual = false,
				DeclaringType = typeof(global::VoyagerApi.Request),
				Converter = null,
				Getter = static obj => ((global::VoyagerApi.Request)obj).PhoneNumbers,
				Setter = static (obj, value) => ((global::VoyagerApi.Request)obj).PhoneNumbers = value!,
				IgnoreCondition = null,
				HasJsonInclude = false,
				IsExtensionData = false,
				NumberHandling = null,
				PropertyName = "PhoneNumbers",
				JsonPropertyName = "phoneNumbers"
			};

			properties[3] = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreatePropertyInfo<global::System.Collections.Generic.IEnumerable<string>>(options, info4);

			return properties;
		}

		public void MapEndpoints(WebApplication app)
		{
			var instRequestValidator = new RequestValidator();
			var modelBinder = new ModelBinder();
			var endpoint = app.Services.GetRequiredService<VoyagerApi.Endpoint>();
			var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
			VoyagerApi.Endpoint.Configure(app.MapPost("/benchmark/ok/{id}", async (HttpContext context) =>
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
				builder.AddBody(typeof(RequestBody));
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(200, typeof(VoyagerApi.Response));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}))()));
			VoyagerApi.Endpoint.Configure(app.MapPost("/benchmark2/ok/{id}", async (HttpContext context) =>
			{
				var body = await JsonSerializer.DeserializeAsync<RequestBody>(context.Request.Body, jsonOptions);
				var request = new VoyagerApi.Request
				{
					Age = body.age,
					FirstName = body.firstName,
					LastName = body.lastName,
					PhoneNumbers = body.phoneNumbers,
					Id = modelBinder.GetNumber<int>(context, ModelBindingSource.Route, "id")
				};
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
				builder.AddBody(typeof(RequestBody));
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(200, typeof(VoyagerApi.Response));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}))()));
		}


		private class RequestBody
		{
			public string firstName { get; set; }
			public string lastName { get; set; }
			public int age { get; set; }
			public System.Collections.Generic.IEnumerable<string> phoneNumbers { get; set; }
		}
		public class RequestValidator : AbstractValidator<VoyagerApi.Request>
		{
			public RequestValidator()
			{
				VoyagerApi.Request.AddValidationRules(this);
			}
		}

	}

	[JsonSerializable(typeof(VoyagerApi.Request))]
	internal partial class SourceGenerationContext : JsonSerializerContext
	{
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
			//services.ConfigureOptions<JsonConfigureOptions>();
		}

		private class JsonConfigureOptions : IConfigureOptions<JsonOptions>
		{
			public void Configure(JsonOptions options)
			{
				var resolver = options.SerializerOptions.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
				options.SerializerOptions.TypeInfoResolver = resolver.WithAddedModifier(RemoveSomething);
			}
		}

		public static void RemoveSomething(JsonTypeInfo typeInfo)
		{
			if (typeInfo.Type == typeof(VoyagerApi.Request))
			{
				//typeInfo.Properties.Remove(typeInfo.Properties.First(p => p.Name == "id"));
				typeInfo.Properties.Clear();
				foreach (var prop in EndpointMapper.CreateTypeInfoProperties(typeInfo.Options))
				{
					typeInfo.Properties.Add(prop);
				}
			}
		}
	}
}
