#nullable enable
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Voyager;
using Voyager.ModelBinding;

namespace Voyager.Generated.VoyagerBench_VoyagerSourceGen
{
	internal class EndpointMapper : Voyager.IVoyagerMapping
	{
		public void MapEndpoints(WebApplication app)
		{
			var modelBinder = app.Services.GetService<IModelBinder>() ?? new ModelBinder();
			var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
			var stringProvider = app.Services.GetService<Voyager.ModelBinding.IStringValuesProvider>() ?? new Voyager.ModelBinding.StringValuesProvider();
			var inst_VoyagerApi_NoRequestEndpoint = app.Services.GetRequiredService<VoyagerApi.NoRequestEndpoint>();
			var inst_VoyagerApi_StaticEndpoint = app.Services.GetRequiredService<VoyagerApi.StaticEndpoint>();
			var inst_VoyagerApi_Endpoint = app.Services.GetRequiredService<VoyagerApi.Endpoint>();
			var instVoyagerApi_EndpointPostValidator = new VoyagerApi_EndpointPostValidator();
			var inst_VoyagerApi_AnonymousEndpoint = app.Services.GetRequiredService<VoyagerApi.AnonymousEndpoint>();
			var inst_VoyagerApi_Duplicate_AnonymousEndpoint = app.Services.GetRequiredService<VoyagerApi.Duplicate.AnonymousEndpoint>();
			var inst_VoyagerApi_MultipleInjections = app.Services.GetRequiredService<VoyagerApi.MultipleInjections>();
			app.MapGet("/norequest", (Microsoft.AspNetCore.Http.HttpContext context) =>
			{
				return Microsoft.AspNetCore.Http.TypedResults.Ok(inst_VoyagerApi_NoRequestEndpoint.Get());
			}
			).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(200, typeof(int));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}
			)());
			app.MapGet("/static", (Microsoft.AspNetCore.Http.HttpContext context) =>
			{
				return (Microsoft.AspNetCore.Http.IResult)(VoyagerApi.StaticEndpoint.Get(context.RequestServices.GetRequiredService<VoyagerApi.Service>()));
			}
			).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.Ok().StatusCode, typeof(VoyagerApi_StaticEndpointGetResponse0));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}
			)());
			VoyagerApi.Endpoint.Configure(app.MapPost("/benchmark/ok/{id}", async (Microsoft.AspNetCore.Http.HttpContext context, [Microsoft.AspNetCore.Mvc.FromRouteAttribute(Name = "id")] int id) =>

			{
				var body = await JsonSerializer.DeserializeAsync<VoyagerApi_EndpointPostRequest>(context.Request.Body, jsonOptions);
				var request = new VoyagerApi.Request
				{
					Id = id,
					FirstName = body?.FirstName ?? null,
					LastName = body?.LastName ?? null,
					Age = body?.Age ?? default!,
					PhoneNumbers = body?.PhoneNumbers ?? null,
				};
				var validationResult = await instVoyagerApi_EndpointPostValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return Microsoft.AspNetCore.Http.Results.ValidationProblem(validationResult.ToDictionary());
				}
				return Microsoft.AspNetCore.Http.TypedResults.Ok(inst_VoyagerApi_Endpoint.Post(request, context.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<VoyagerApi.Program>>()));
			}
			).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddParameter("id", Microsoft.OpenApi.Models.ParameterLocation.Path, typeof(int), false);
				builder.AddBody(typeof(VoyagerApi_EndpointPostRequest));
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(200, typeof(VoyagerApi.Response));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}
			)()));
			app.MapGet("/anonymous", async (Microsoft.AspNetCore.Http.HttpContext context) =>
			{
				var body = await JsonSerializer.DeserializeAsync<VoyagerApi_AnonymousEndpointGetRequest>(context.Request.Body, jsonOptions);
				var request = new VoyagerApi.AnonymousEndpoint.Body
				{
					Test = body?.Test ?? null,
				};
				return (Microsoft.AspNetCore.Http.IResult)(inst_VoyagerApi_AnonymousEndpoint.Get(request));
			}
			).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddBody(typeof(VoyagerApi_AnonymousEndpointGetRequest));
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.Ok().StatusCode, typeof(VoyagerApi_AnonymousEndpointGetResponse0));
				builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.Ok().StatusCode, typeof(VoyagerApi_AnonymousEndpointGetResponse1));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}
			)());
			app.MapGet("/duplicate/anonymous", async (Microsoft.AspNetCore.Http.HttpContext context) =>
			{
				var body = await JsonSerializer.DeserializeAsync<VoyagerApi_Duplicate_AnonymousEndpointGetRequest>(context.Request.Body, jsonOptions);
				var request = new VoyagerApi.Duplicate.AnonymousEndpoint.Body
				{
					Test = body?.Test ?? null,
					Value = body?.Value ?? default!,
				};
				return (Microsoft.AspNetCore.Http.IResult)(inst_VoyagerApi_Duplicate_AnonymousEndpoint.Get(request));
			}
			).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddBody(typeof(VoyagerApi_Duplicate_AnonymousEndpointGetRequest));
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.Ok().StatusCode, typeof(VoyagerApi_Duplicate_AnonymousEndpointGetResponse0));
				builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.Ok().StatusCode, typeof(VoyagerApi_Duplicate_AnonymousEndpointGetResponse1));
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}
			)());
			app.MapGet("/multipleInjections", (Microsoft.AspNetCore.Http.HttpContext context) =>
			{
				return (Microsoft.AspNetCore.Http.IResult)(inst_VoyagerApi_MultipleInjections.Get(context.RequestServices.GetRequiredService<VoyagerApi.Service>()));
			}
			).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.Ok().StatusCode, null);
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}
			)());
			app.MapPost("/multipleInjections", (Microsoft.AspNetCore.Http.HttpContext context) =>
			{
				return (Microsoft.AspNetCore.Http.IResult)(inst_VoyagerApi_MultipleInjections.Post(context.RequestServices.GetRequiredService<VoyagerApi.Service>()));
			}
			).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() =>
			{
				var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
				builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));
				builder.AddResponse(Microsoft.AspNetCore.Http.TypedResults.Ok().StatusCode, null);
				return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
			}
			)());
		}
#pragma warning disable CS8618
		private class VoyagerApi_StaticEndpointGetResponse0
		{
			public bool test { get; set; }
		}
#pragma warning restore CS8618
#pragma warning disable CS8618
		private class VoyagerApi_EndpointPostRequest
		{
			public string? FirstName { get; set; }
			public string? LastName { get; set; }
			public int Age { get; set; }
			public System.Collections.Generic.IEnumerable<string>? PhoneNumbers { get; set; }
		}
#pragma warning restore CS8618
		public class VoyagerApi_EndpointPostValidator : AbstractValidator<VoyagerApi.Request>
		{
			public VoyagerApi_EndpointPostValidator()
			{
				VoyagerApi.Request.Validate(this);
			}
		}
#pragma warning disable CS8618
		private class VoyagerApi_AnonymousEndpointGetRequest
		{
			public string? Test { get; set; }
		}
#pragma warning restore CS8618
#pragma warning disable CS8618
		private class VoyagerApi_AnonymousEndpointGetResponse0
		{
			public string something { get; set; }
		}
#pragma warning restore CS8618
#pragma warning disable CS8618
		private class VoyagerApi_AnonymousEndpointGetResponse1
		{
			public string? result { get; set; }
		}
#pragma warning restore CS8618
#pragma warning disable CS8618
		private class VoyagerApi_Duplicate_AnonymousEndpointGetRequest
		{
			public string? Test { get; set; }
			public int Value { get; set; }
		}
#pragma warning restore CS8618
#pragma warning disable CS8618
		private class VoyagerApi_Duplicate_AnonymousEndpointGetResponse0
		{
			public string something { get; set; }
		}
#pragma warning restore CS8618
#pragma warning disable CS8618
		private class VoyagerApi_Duplicate_AnonymousEndpointGetResponse1
		{
			public string? result { get; set; }
		}
#pragma warning restore CS8618
	}
}

namespace Microsoft.Extensions.DependencyInjection
{
	internal static class VoyagerEndpoints
	{
		internal static void AddVoyager(this IServiceCollection services)
		{
			services.AddTransient<VoyagerApi.NoRequestEndpoint>();
			services.AddTransient<VoyagerApi.Endpoint>();
			services.AddTransient<VoyagerApi.AnonymousEndpoint>();
			services.AddTransient<VoyagerApi.Duplicate.AnonymousEndpoint>();
			services.AddTransient<VoyagerApi.MultipleInjections>();
			services.AddTransient<IVoyagerMapping, Voyager.Generated.VoyagerBench_VoyagerSourceGen.EndpointMapper>();
		}
	}
}
