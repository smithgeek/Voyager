#nullable enable annotations
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Voyager;
using Voyager.Extensions;
using Voyager.ModelBinding;
using Voyager.OpenApi;
using Voyager.Generation;
using Voyager.Validation;
using Microsoft.OpenApi.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Voyager.g
{
    public class ClassLibrary_EndpointMappingsDebug : Voyager.IVoyagerMapping
    {
        public void MapEndpoints(WebApplication app)
        {
            var validatorFactory = app.Services.GetRequiredService<VoyagerUnifiedValidationFactory>();
            var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
            var modelBinder = app.Services.GetService<IModelBinder>() ?? new ModelBinder();
            var stringProvider = app.Services.GetService<Voyager.ModelBinding.IStringValuesProvider>() ?? new  Voyager.ModelBinding.StringValuesProvider();
            var transformerRepo = app.Services.GetRequiredService<OpenApiTransformerRepo>();
            var inst_ClassLibrary_Handler = app.Services.GetRequiredService<ClassLibrary.Handler>();
            var validator_ClassLibrary_Request = validatorFactory.Create<ClassLibrary.Request>(true);
            validator_ClassLibrary_Request.NotNull(r => r.Name, "Name");
            var inst_ClassLibrary_Handler2 = app.Services.GetRequiredService<ClassLibrary.Handler2>();
            transformerRepo.AddRequestBody<ClassLibrary.Request2>(["Index"]);
            var validator_ClassLibrary_Request2 = validatorFactory.Create<ClassLibrary.Request2>(true);
            validator_ClassLibrary_Request2.NotNull(r => r.Name, "Name");
            var inst_ClassLibrary_EmptyRequestHandler = app.Services.GetRequiredService<ClassLibrary.EmptyRequestHandler>();
            var inst_ClassLibrary_GenericRequestHandler = app.Services.GetRequiredService<ClassLibrary.GenericRequestHandler>();
            var validator_ClassLibrary_SupabasePayload_ClassLibrary_Display_ = validatorFactory.Create<ClassLibrary.SupabasePayload<ClassLibrary.Display>>(true);
            validator_ClassLibrary_SupabasePayload_ClassLibrary_Display_.NotNull(r => r.Table, "Table");
            validator_ClassLibrary_SupabasePayload_ClassLibrary_Display_.NotNull(r => r.Schema, "Schema");
            app.MapPost("/test", async (HttpContext context) =>
            {
                using var body = await JsonDocument.ParseAsync(context.Request.Body);
                var request = new ClassLibrary.Request
                {
                    Name = body.MaybeGetString("Name")!,
                };
                var validationResult = await validator_ClassLibrary_Request.Validate(request, propName => propName switch
                {
                    _ => propName
                });
                if(!validationResult.IsValid)
                {
                    return (IResult)TypedResults.ValidationProblem(validationResult.Errors);
                }
                return (IResult)TypedResults.Ok(inst_ClassLibrary_Handler.Post(request));
            }
            ).ProducesValidationProblem(400).Accepts<ClassLibrary.Request>("application/json").Produces<bool>(200)

            ;
            app.MapPost("/test/{index}", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromRoute]int index) =>
            {
                using var body = await JsonDocument.ParseAsync(context.Request.Body);
                var request = new ClassLibrary.Request2
                {
                    Name = body.MaybeGetString("Name")!,
                    Index = index,
                };
                var validationResult = await validator_ClassLibrary_Request2.Validate(request, propName => propName switch
                {
                    "Index" => "index",
                    _ => propName
                });
                return (IResult)TypedResults.Ok(inst_ClassLibrary_Handler2.Post(request, validationResult));
            }
            ).ProducesValidationProblem(400).Accepts<ClassLibrary.Request2>("application/json").Produces<bool>(200)

            ;
            app.MapGet("/test/noreq",  (HttpContext context) =>
            {
                return (IResult)TypedResults.Ok(inst_ClassLibrary_EmptyRequestHandler.Get(new ClassLibrary.EmptyRequest()));
            }
            ).Accepts<ClassLibrary.EmptyRequest>("application/json").Produces<bool>(200)

            ;
            app.MapPost("/generic/req", async (HttpContext context) =>
            {
                using var body = await JsonDocument.ParseAsync(context.Request.Body);
                var request = new ClassLibrary.SupabasePayload<ClassLibrary.Display>
                {
                    Type = body.MaybeGetDeserialize<ClassLibrary.EventType>("Type")!,
                    Table = body.MaybeGetString("Table")!,
                    Schema = body.MaybeGetString("Schema")!,
                    Record = body.MaybeGetDeserialize<ClassLibrary.Display?>("Record"),
                    OldRecord = body.MaybeGetDeserialize<ClassLibrary.Display?>("OldRecord"),
                };
                var validationResult = await validator_ClassLibrary_SupabasePayload_ClassLibrary_Display_.Validate(request, propName => propName switch
                {
                    _ => propName
                });
                if(!validationResult.IsValid)
                {
                    return (IResult)TypedResults.ValidationProblem(validationResult.Errors);
                }
                return (IResult)TypedResults.Ok(inst_ClassLibrary_GenericRequestHandler.Post(request));
            }
            ).ProducesValidationProblem(400).Accepts<ClassLibrary.SupabasePayload<ClassLibrary.Display>>("application/json").Produces<bool>(200)

            ;
        }
        public static void AddServices(IServiceCollection services)
        {
            services.AddTransient<ClassLibrary.Handler>();
            services.AddTransient<ClassLibrary.Handler2>();
            services.AddTransient<ClassLibrary.EmptyRequestHandler>();
            services.AddTransient<ClassLibrary.GenericRequestHandler>();
            services.AddTransient<IVoyagerMapping, Voyager.g.ClassLibrary_EndpointMappingsDebug>();
        }
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        #pragma warning disable IDE1006 // Naming Styles
        private class ClassLibrary_Handler2PostRequestBody
        {
            public string? Name { get; set; } 
        }
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        #pragma warning restore IDE1006 // Naming Styles
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class VoyagerEndpointsDebug
    {
        internal static void AddVoyagerDebug(this IServiceCollection services, Action<VoyagerConfig>? configure = null)
        {
            var config = new VoyagerConfig(services);
            configure?.Invoke(config);
            services.TryAddSingleton<OpenApiTransformerRepo>();
            services.TryAddTransient<VoyagerUnifiedValidationFactory>();
            Voyager.g.ClassLibrary_EndpointMappingsDebug.AddServices(services);
            Voyager.g.TransitiveClassLibrary_EndpointMappings.AddServices(services);
            config.RegisterAssemblyWithType<Voyager.g.TransitiveClassLibrary_EndpointMappings>();
            config.RegisterAssemblyWithType<Voyager.g.ClassLibrary_EndpointMappingsDebug>();
            config.FinishedRegisteringAssemblies(services);
        }
    }
}
