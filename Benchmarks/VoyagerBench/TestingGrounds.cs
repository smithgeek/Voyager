#nullable enable
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Voyager;
using Voyager.Extensions;
using Voyager.ModelBinding;
using Microsoft.OpenApi.Models;

namespace Voyager.Generated.VoyagerBench_VoyagerSourceGen
{
    internal class EndpointMapperDebug : Voyager.IVoyagerMapping
    {
        public void MapEndpoints(WebApplication app)
        {
            var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
            var modelBinder = app.Services.GetService<IModelBinder>() ?? new ModelBinder();
            var stringProvider = app.Services.GetService<Voyager.ModelBinding.IStringValuesProvider>() ?? new  Voyager.ModelBinding.StringValuesProvider();
            var inst_VoyagerApi_NoRequestEndpoint = app.Services.GetRequiredService<VoyagerApi.NoRequestEndpoint>();
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_StaticEndpointGetResponse1>("GetStaticResponse");
            var inst_VoyagerApi_Endpoint = app.Services.GetRequiredService<VoyagerApi.Endpoint>();
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_EndpointPostRequestBody>("PostBenchmarkOkRequest");
            var validator_VoyagerApi_Request = new Voyager.Validation.GenericValidator<VoyagerApi.Request>();
            validator_VoyagerApi_Request.RuleFor(r => r.LastName).NotNull();
            validator_VoyagerApi_Request.RuleFor(r => r.Complexes).NotNull();
            VoyagerApi.Request.Validate(validator_VoyagerApi_Request);
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_EndpointPostResponse1>("PostBenchmarkOkResponse");
            var inst_VoyagerApi_ValidotEndpoint = app.Services.GetRequiredService<VoyagerApi.ValidotEndpoint>();
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_ValidotEndpointPostRequestBody>("PostValidotBenchmarkOkRequest");
            var validator_VoyagerApi_ValidotRequest = VoyagerApi.ValidotRequest.CreateValidator();
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_ValidotEndpointPostResponse1>("PostValidotBenchmarkOkResponse");
            var inst_VoyagerApi_AnonymousEndpoint = app.Services.GetRequiredService<VoyagerApi.AnonymousEndpoint>();
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi.AnonymousEndpoint.Body>("GetAnonymousRequest");
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_AnonymousEndpointGetResponse1>("GetAnonymousResponse");
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_AnonymousEndpointGetResponse2>("GetAnonymousResponse2");
            var inst_VoyagerApi_Duplicate_AnonymousEndpoint = app.Services.GetRequiredService<VoyagerApi.Duplicate.AnonymousEndpoint>();
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi.Duplicate.AnonymousEndpoint.Body>("GetDuplicateAnonymousRequest");
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_Duplicate_AnonymousEndpointGetResponse1>("GetDuplicateAnonymousResponse");
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_Duplicate_AnonymousEndpointGetResponse2>("GetDuplicateAnonymousResponse2");
            var inst_VoyagerApi_MultipleInjections = app.Services.GetRequiredService<VoyagerApi.MultipleInjections>();
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_RecordsEndpointGetRequestBody>("GetRecordsRequest");
            var validator_VoyagerApi_RecordsEndpoint_Request = new Voyager.Validation.GenericValidator<VoyagerApi.RecordsEndpoint.Request>();
            validator_VoyagerApi_RecordsEndpoint_Request.RuleFor(r => r.Id).NotNull();
            validator_VoyagerApi_RecordsEndpoint_Request.RuleFor(r => r.Name).NotNull();
            VoyagerApi.RecordsEndpoint.Request.Validate(validator_VoyagerApi_RecordsEndpoint_Request);
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_RecordsEndpointGetResponse1>("GetRecordsResponse");
            Voyager.OpenApi.SchemaIdMapping.Add<VoyagerApi_RecordsEndpointDeleteRequestBody>("DeleteRecordsRequest");
            app.MapGet("/norequest",  (HttpContext context) =>
            {
                return TypedResults.Ok(inst_VoyagerApi_NoRequestEndpoint.Get());
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(200, typeof(int));
                builder.AddResponse(200,
                new OpenApiSchema { Type = "integer", Format = "int32", Nullable = true }
                );
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )());
            app.MapGet("/static",  (HttpContext context) =>
            {
                return (IResult)(VoyagerApi.StaticEndpoint.Get(context.RequestServices.GetRequiredService<VoyagerApi.Service>()));
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(TypedResults.Ok().StatusCode, typeof(VoyagerApi_StaticEndpointGetResponse1));
                builder.AddResponse(TypedResults.Ok().StatusCode,
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "test",
                            new OpenApiSchema { Type = "boolean" }
                        },
                    }
                }
                );
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )());
            VoyagerApi.Endpoint.Configure(app.MapPost("/benchmark/ok/{id}", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromRoute(Name = "id")]int id) =>

            {
                var body = await JsonSerializer.DeserializeAsync<VoyagerApi_EndpointPostRequestBody>(context.Request.Body, jsonOptions);
                var request = new VoyagerApi.Request(body?.FirstName ?? null,body?.LastName ?? default!)
                {
                    UserId = id,
                    Age = body?.Age ?? null,
                    PhoneNumbers = body?.PhoneNumbers ?? null,
                    Complexes = body?.Complexes ?? default!,
                };
                var validationResult = await validator_VoyagerApi_Request.ValidateAsync(request);
                if(!validationResult.IsValid)
                {
                    var dictionary = validationResult.ToDictionary();
                    dictionary.ReplaceKey("UserId", "id");
                    return Results.ValidationProblem(dictionary);
                }
                return TypedResults.Ok(inst_VoyagerApi_Endpoint.Post(request, context.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<VoyagerApi.Program>>()));
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddParameter("id", Microsoft.OpenApi.Models.ParameterLocation.Path, typeof(int), false);
                builder.AddBody(
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "FirstName",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                        
                        {
                            "LastName",
                            new OpenApiSchema { Type = "string" }
                        },
                        
                        {
                            "Age",
                            new OpenApiSchema { Type = "integer", Format = "int32", Nullable = true }
                        },
                        
                        {
                            "PhoneNumbers",
                            new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "string", Nullable = true }, Nullable = true }
                        },
                        
                        {
                            "Complexes",
                            new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Reference = new OpenApiReference { Id = "Complex" }, } }
                        },
                    }
                }
                );
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(200, typeof(VoyagerApi_EndpointPostResponse1));
                builder.AddResponse(200,
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "Id",
                            new OpenApiSchema { Type = "integer", Format = "int32" }
                        },
                        
                        {
                            "Name",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                        
                        {
                            "Age",
                            new OpenApiSchema { Type = "integer", Format = "int32" }
                        },
                        
                        {
                            "PhoneNumber",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                    }
                }
                );
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )()));
            VoyagerApi.ValidotEndpoint.Configure(app.MapPost("/validot/benchmark/ok/{id}", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromRoute(Name = "id")]int id) =>

            {
                var body = await JsonSerializer.DeserializeAsync<VoyagerApi_ValidotEndpointPostRequestBody>(context.Request.Body, jsonOptions);
                var request = new VoyagerApi.ValidotRequest(body?.FirstName ?? null)
                {
                    UserId = id,
                    LastName = body?.LastName ?? null,
                    Age = body?.Age ?? default!,
                    PhoneNumbers = body?.PhoneNumbers ?? null,
                };
                var validationResult = validator_VoyagerApi_ValidotRequest.Validate(request);
                return TypedResults.Ok(inst_VoyagerApi_ValidotEndpoint.Post(request, context.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<VoyagerApi.Program>>(), validationResult));
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddParameter("id", Microsoft.OpenApi.Models.ParameterLocation.Path, typeof(int), false);
                builder.AddBody(
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "FirstName",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                        
                        {
                            "LastName",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                        
                        {
                            "Age",
                            new OpenApiSchema { Type = "integer", Format = "int32" }
                        },
                        
                        {
                            "PhoneNumbers",
                            new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "string" }, Nullable = true }
                        },
                    }
                }
                );
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(200, typeof(VoyagerApi_ValidotEndpointPostResponse1));
                builder.AddResponse(200,
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "Id",
                            new OpenApiSchema { Type = "integer", Format = "int32" }
                        },
                        
                        {
                            "Name",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                        
                        {
                            "Age",
                            new OpenApiSchema { Type = "integer", Format = "int32" }
                        },
                        
                        {
                            "PhoneNumber",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                    }
                }
                );
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )()));
            app.MapGet("/anonymous", async (HttpContext context) =>
            {
                var request = await JsonSerializer.DeserializeAsync<VoyagerApi.AnonymousEndpoint.Body>(context.Request.Body, jsonOptions);
                if(request == null)
                {
                    return TypedResults.Problem("Unable to parse request body", statusCode: 400);
                }
                return (IResult)(inst_VoyagerApi_AnonymousEndpoint.Get(request));
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddBody(
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "Test",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                    }
                }
                );
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(TypedResults.Ok().StatusCode, typeof(VoyagerApi_AnonymousEndpointGetResponse1));
                builder.AddResponse(TypedResults.Ok().StatusCode,
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "something",
                            new OpenApiSchema { Type = "string" }
                        },
                    }
                }
                );
                builder.AddResponse(TypedResults.Ok().StatusCode, typeof(VoyagerApi_AnonymousEndpointGetResponse2));
                builder.AddResponse(TypedResults.Ok().StatusCode,
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "result",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                    }
                }
                );
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )());
            app.MapGet("/duplicate/anonymous", async (HttpContext context) =>
            {
                var request = await JsonSerializer.DeserializeAsync<VoyagerApi.Duplicate.AnonymousEndpoint.Body>(context.Request.Body, jsonOptions);
                if(request == null)
                {
                    return TypedResults.Problem("Unable to parse request body", statusCode: 400);
                }
                return (IResult)(inst_VoyagerApi_Duplicate_AnonymousEndpoint.Get(request));
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddBody(
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "Test",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                        
                        {
                            "Value",
                            new OpenApiSchema { Type = "integer", Format = "int32" }
                        },
                    }
                }
                );
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(TypedResults.Ok().StatusCode, typeof(VoyagerApi_Duplicate_AnonymousEndpointGetResponse1));
                builder.AddResponse(TypedResults.Ok().StatusCode,
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "something",
                            new OpenApiSchema { Type = "string" }
                        },
                    }
                }
                );
                builder.AddResponse(TypedResults.Ok().StatusCode, typeof(VoyagerApi_Duplicate_AnonymousEndpointGetResponse2));
                builder.AddResponse(TypedResults.Ok().StatusCode,
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "result",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                    }
                }
                );
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )());
            app.MapGet("/multipleInjections",  (HttpContext context) =>
            {
                return (IResult)(inst_VoyagerApi_MultipleInjections.Get(context.RequestServices.GetRequiredService<VoyagerApi.Service>()));
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(TypedResults.Ok().StatusCode, null);
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )());
            app.MapPost("/multipleInjections",  (HttpContext context) =>
            {
                return (IResult)(inst_VoyagerApi_MultipleInjections.Post(context.RequestServices.GetRequiredService<VoyagerApi.Service>()));
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(TypedResults.Ok().StatusCode, null);
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )());
            app.MapGet("/records", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromQuery]string id, [Microsoft.AspNetCore.Mvc.FromQuery]int days = 5) =>
            {
                var body = await JsonSerializer.DeserializeAsync<VoyagerApi_RecordsEndpointGetRequestBody>(context.Request.Body, jsonOptions);
                var request = new VoyagerApi.RecordsEndpoint.Request(id,body?.Value ?? default!,body?.Text ?? null,body?.Name ?? default!,days)
                {
                    Other = body?.Other ?? null,
                };
                var validationResult = await validator_VoyagerApi_RecordsEndpoint_Request.ValidateAsync(request);
                if(!validationResult.IsValid)
                {
                    var dictionary = validationResult.ToDictionary();
                    dictionary.ReplaceKey("Id", "id");
                    return Results.ValidationProblem(dictionary);
                }
                return TypedResults.Ok(VoyagerApi.RecordsEndpoint.Get(request));
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddParameter("id", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(string), true);
                builder.AddParameter("days", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(int), false);
                builder.AddBody(
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "Value",
                            new OpenApiSchema { Type = "integer", Format = "int32" }
                        },
                        
                        {
                            "Text",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                        
                        {
                            "Name",
                            new OpenApiSchema { Type = "string" }
                        },
                        
                        {
                            "Other",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                    }
                }
                );
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(200, typeof(VoyagerApi_RecordsEndpointGetResponse1));
                builder.AddResponse(200,
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "Value",
                            new OpenApiSchema { Type = "string" }
                        },
                    }
                }
                );
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )());
            app.MapDelete("/records", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromQuery]string id, [Microsoft.AspNetCore.Mvc.FromQuery]int days = 5) =>
            {
                var body = await JsonSerializer.DeserializeAsync<VoyagerApi_RecordsEndpointDeleteRequestBody>(context.Request.Body, jsonOptions);
                var request = new VoyagerApi.RecordsEndpoint.Request(id,body?.Value ?? default!,body?.Text ?? null,body?.Name ?? default!,days)
                {
                    Other = body?.Other ?? null,
                };
                var validationResult = await validator_VoyagerApi_RecordsEndpoint_Request.ValidateAsync(request);
                if(!validationResult.IsValid)
                {
                    var dictionary = validationResult.ToDictionary();
                    dictionary.ReplaceKey("Id", "id");
                    return Results.ValidationProblem(dictionary);
                }
                return TypedResults.Ok(VoyagerApi.RecordsEndpoint.Delete(request));
            }
            ).WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => 
            {
                var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());
                builder.AddParameter("id", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(string), true);
                builder.AddParameter("days", Microsoft.OpenApi.Models.ParameterLocation.Query, typeof(int), false);
                builder.AddBody(
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                    {
                        
                        {
                            "Value",
                            new OpenApiSchema { Type = "integer", Format = "int32" }
                        },
                        
                        {
                            "Text",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                        
                        {
                            "Name",
                            new OpenApiSchema { Type = "string" }
                        },
                        
                        {
                            "Other",
                            new OpenApiSchema { Type = "string", Nullable = true }
                        },
                    }
                }
                );
                builder.AddResponse(400, typeof(HttpValidationProblemDetails));
                builder.AddResponse(200, typeof(bool));
                builder.AddResponse(200,
                new OpenApiSchema { Type = "boolean", Nullable = true }
                );
                return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };
            }
            )());
        }
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        #pragma warning disable IDE1006 // Naming Styles
        private class VoyagerApi_StaticEndpointGetResponse1
        {
            public bool test { get; set; } 
        }
        private class VoyagerApi_EndpointPostRequestBody
        {
            public string? FirstName { get; set; } 
            public string LastName { get; set; } 
            public int? Age { get; set; } 
            public System.Collections.Generic.IEnumerable<string?>? PhoneNumbers { get; set; } 
            public System.Collections.Generic.List<VoyagerApi.Complex> Complexes { get; set; } 
        }
        private class VoyagerApi_EndpointPostResponse1
        {
            public int Id { get; set; } 
            public string? Name { get; set; } 
            public int Age { get; set; } 
            public string? PhoneNumber { get; set; } 
        }
        private class VoyagerApi_ValidotEndpointPostRequestBody
        {
            public string? FirstName { get; set; } 
            public string? LastName { get; set; } 
            public int Age { get; set; } 
            public System.Collections.Generic.IEnumerable<string>? PhoneNumbers { get; set; } 
        }
        private class VoyagerApi_ValidotEndpointPostResponse1
        {
            public int Id { get; set; } 
            public string? Name { get; set; } 
            public int Age { get; set; } 
            public string? PhoneNumber { get; set; } 
        }
        private class VoyagerApi_AnonymousEndpointGetResponse1
        {
            public string something { get; set; } 
        }
        private class VoyagerApi_AnonymousEndpointGetResponse2
        {
            public string? result { get; set; } 
        }
        private class VoyagerApi_Duplicate_AnonymousEndpointGetResponse1
        {
            public string something { get; set; } 
        }
        private class VoyagerApi_Duplicate_AnonymousEndpointGetResponse2
        {
            public string? result { get; set; } 
        }
        private class VoyagerApi_RecordsEndpointGetRequestBody
        {
            public int Value { get; set; } 
            public string? Text { get; set; } 
            public string Name { get; set; } 
            public string? Other { get; set; } 
        }
        private class VoyagerApi_RecordsEndpointGetResponse1
        {
            public string Value { get; set; } 
        }
        private class VoyagerApi_RecordsEndpointDeleteRequestBody
        {
            public int Value { get; set; } 
            public string? Text { get; set; } 
            public string Name { get; set; } 
            public string? Other { get; set; } 
        }
        #pragma warning restore CS8618
        #pragma warning restore IDE1006
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class VoyagerEndpoints
    {
        internal static void AddVoyagerDebug(this IServiceCollection services)
        {
            services.AddTransient<VoyagerApi.NoRequestEndpoint>();
            services.AddTransient<VoyagerApi.Endpoint>();
            services.AddTransient<VoyagerApi.ValidotEndpoint>();
            services.AddTransient<VoyagerApi.AnonymousEndpoint>();
            services.AddTransient<VoyagerApi.Duplicate.AnonymousEndpoint>();
            services.AddTransient<VoyagerApi.MultipleInjections>();
            services.AddTransient<IVoyagerMapping, Voyager.Generated.VoyagerBench_VoyagerSourceGen.EndpointMapper>();
        }
    }
}
