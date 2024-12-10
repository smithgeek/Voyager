using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Voyager.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class VoyagerSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(IsSyntaxTargetForGeneration, GetSemanticTargetForGeneration)
			.Where(static c => c is not null);

		var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());
		context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Left, source.Right!, spc));
	}

	private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken _token)
	{
		return node is ClassDeclarationSyntax @class && @class.AttributeLists.Count > 0;
	}

	private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
	{
		if (classes.IsDefaultOrEmpty)
		{
			return;
		}

		var endpointClasses = classes.Distinct().Select(c =>
		{
			var semanticModel = compilation.GetSemanticModel(c.SyntaxTree);
			var attribute = GetVoyagerEndpointAttribute(c, semanticModel);
			if (attribute != null)
			{
				return new EndpointClass(c, semanticModel, attribute);
			}
			return null;
		}).Where(c => c is not null).Select(c => c!);

		var emitter = new SourceEmitter();
		var code = emitter.Emit(endpointClasses, compilation,
#if DEBUG
			Debugger.IsAttached ? "Debug" : string.Empty
#else
			string.Empty
#endif
		);
#if DEBUG
		if (Debugger.IsAttached)
		{
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
			System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "TestingGrounds.cs"), code);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
		}
#endif
		context.AddSource($"{compilation.AssemblyName}.Voyager.EndpointMapper.g.cs", code);
	}

	private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken _token)
	{
		var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
		var voyagerAttribute = GetVoyagerEndpointAttribute(classDeclarationSyntax, context.SemanticModel);
		if (voyagerAttribute != null)
		{
			return classDeclarationSyntax;
		}
		return null;
	}

	private static AttributeSyntax? GetVoyagerEndpointAttribute(ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
	{
		foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
		{
			foreach (var attributeSyntax in attributeListSyntax.Attributes)
			{
				var attributeSymbol = semanticModel.GetSymbolInfo(attributeSyntax).Symbol;
				if (attributeSymbol == null)
				{
					continue;
				}

				var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
				var fullName = attributeContainingTypeSymbol.ToDisplayString();
				if (fullName == "Voyager.VoyagerEndpointAttribute")
				{
					return attributeSyntax;
				}
			}
		}
		return null;
	}
}

internal class SourceEmitter
{
	internal string Emit(IEnumerable<EndpointClass> endpointClasses, Compilation compilation, string debugSuffix)
	{
		var source = new SourceBuilder();
		source.AddDirective("#nullable enable annotations")
			.AddUsing("Microsoft.AspNetCore.Http")
			.AddUsing("Microsoft.AspNetCore.Builder")
			.AddUsing("Microsoft.AspNetCore.Http.Json")
			.AddUsing("Microsoft.Extensions.DependencyInjection")
			.AddUsing("Microsoft.Extensions.Options")
			.AddUsing("System.Text.Json")
			.AddUsing("Voyager")
			.AddUsing("Voyager.Extensions")
			.AddUsing("Voyager.ModelBinding")
			.AddUsing("Voyager.OpenApi")
			.AddUsing("Voyager.Generation")
			.AddUsing("Microsoft.OpenApi.Models")
			.AddUsing("System.ComponentModel.DataAnnotations")
			.AddUsing("Microsoft.Extensions.DependencyInjection.Extensions");

		var fluentValidationAdded = false;
		void AddFluentValidation()
		{
			if (!fluentValidationAdded)
			{
				source.AddUsing("FluentValidation");
				fluentValidationAdded = true;
			}
		}

		var generatedNamespace = $"Voyager.Generated.Assemblies.g{compilation.AssemblyName}";
		var voyagerGenNs = source.AddNamespace(generatedNamespace);
		var servicesMethod = source.AddNamespace("Microsoft.Extensions.DependencyInjection")
			.AddClass(new($"VoyagerEndpoints{debugSuffix}", Access.Internal, isStatic: true))
			.AddMethod(new($"AddVoyager{debugSuffix}", access: Access.Internal, isStatic: true))
			.AddParameter("this IServiceCollection services")
			.AddStatement("services.TryAddSingleton<OpenApiTransformerRepo>();");
		var endpointMapper = voyagerGenNs
			.AddClass(new($"EndpointMapper{debugSuffix}", Access.Public))
			.AddBase("Voyager.IVoyagerMapping");
		var mapEndpoints = endpointMapper
			.AddMethod(new("MapEndpoints", access: Access.Public))
			.AddParameter("WebApplication app");
		var genericValidatorAdded = false;

		var endpointsInitRegion = mapEndpoints.AddRegion();
		endpointsInitRegion.AddStatement("var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;");
		endpointsInitRegion.AddStatement("var modelBinder = app.Services.GetService<IModelBinder>() ?? new ModelBinder();");
		endpointsInitRegion.AddStatement("var stringProvider = app.Services.GetService<Voyager.ModelBinding.IStringValuesProvider>() ?? new  Voyager.ModelBinding.StringValuesProvider();");
		endpointsInitRegion.AddStatement("var transformerRepo = app.Services.GetRequiredService<OpenApiTransformerRepo>();");

		var classesRegion = endpointMapper.AddRegion()
			.AddDirective("#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.",
			"#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.")
			.AddDirective("#pragma warning disable IDE1006 // Naming Styles", "#pragma warning restore IDE1006 // Naming Styles");

		var validationsAdded = new HashSet<string>();
		var counter = 0;

		foreach (var endpointClass in endpointClasses)
		{
			if (endpointClass.CanBeSingleton && !endpointClass.IsStatic)
			{
				endpointsInitRegion.AddStatement($"var {endpointClass.InstanceName} = app.Services.GetRequiredService<{endpointClass.FullName}>();");
			}
			if (!endpointClass.IsStatic)
			{
				servicesMethod.AddStatement($"services.AddTransient<{endpointClass.FullName}>();");
			}
			foreach (var endpoint in endpointClass.EndpointMethods)
			{
				var request = endpoint.Request;

				if (request != null)
				{
					if (request.Properties.Any(p => p.DataSource != ModelBindingSource.Body))
					{
						GenerateRequestBodyClasses(classesRegion, request);
						var exclude = request.Properties.Where(p => p.DataSource != ModelBindingSource.Body).Select(p => $"\"{p.Name}\"");
						endpointsInitRegion.AddStatement($"transformerRepo.AddRequestBody<{request.FullName}>([{string.Join(", ", exclude)}]);");
					}
				}

				if (endpointClass.HasConfigureMethod)
				{
					mapEndpoints.AddPartialStatement($"{endpointClass.FullName}.Configure(");
				}
				var minimalApiParams = new[] { "HttpContext context" }.Concat(request?.PropertyValuesFromMinimalApi ?? []);
				mapEndpoints.AddStatement($"app.Map{endpoint.HttpMethod}({endpointClass.Path}, {(endpoint.NeedsAsync ? "async" : "")} ({string.Join(", ", minimalApiParams)}) =>");
				var mapContent = mapEndpoints.AddScope();


				if (!endpointClass.CanBeSingleton && !endpoint.IsStatic)
				{
					mapContent.AddStatement($"var {endpointClass.InstanceName} = context.RequestServices.GetRequiredService<{endpointClass.FullName}>();");
				}
				foreach (var property in endpointClass.GetPropertiesNeedingInjected())
				{
					mapContent.AddStatement($"{endpointClass.InstanceName}.{property.Name} = {property.Type.GetInstanceOf()};");
				}
				if (request?.HasBody ?? false)
				{
					if (request.NeedsBodyClassGenerated)
					{
						mapContent.AddStatement($"using var body = await JsonDocument.ParseAsync(context.Request.Body);");
					}
					else
					{
						mapContent.AddStatement($"var request = await JsonSerializer.DeserializeAsync<{request.BodyClass}>(context.Request.Body, jsonOptions);");
						var @if = mapContent.AddIf("request == null");
						@if.AddStatement("return (IResult)TypedResults.Problem(\"Unable to parse request body\", statusCode: 400);");
					}
				}
				if (request != null && request.NeedsBodyClassGenerated)
				{
					var constructor = string.Empty;
					var constructorParams = request.Properties.Where(p => p.ConstructorIndex.HasValue);
					if (constructorParams.Any())
					{
						constructor = $"({string.Join(",", constructorParams.Select(p => p.GetInitValue()))})";
					}
					var requestInit = mapContent.AddScope($"var request = new {request.FullName}{constructor}", ";");
					foreach (var property in request.Properties.Where(p => !p.ConstructorIndex.HasValue))
					{
						requestInit.AddStatement($"{property.Property.Name} = {property.GetInitValue()},");
					}
				}
				var awaitCode = endpoint.IsTask ? "await " : "";

				var parameters = endpoint.GetInjectedParameters();
				if (request?.NeedsValidating ?? false)
				{
					var validatorVariableName = $"validator_{request.FullName.Replace(".", "_")}";
					if (!validationsAdded.Contains(validatorVariableName))
					{
						validationsAdded.Add(validatorVariableName);
						if (request.ValidationMode == ValidationMode.FluentValidation)
						{
							AddFluentValidation();
							endpointsInitRegion.AddStatement($"var {validatorVariableName} = new GenericValidator<{request.FullName}>();");
							CallValidation(endpointsInitRegion, request, validatorVariableName);
							if (!genericValidatorAdded)
							{
								genericValidatorAdded = true;
								endpointMapper.AddClass(new("GenericValidator<T>", Access.Private)).AddBase("FluentValidation.AbstractValidator<T>");
							}
						}
						else if (request.ValidationMode == ValidationMode.Validot)
						{
							endpointsInitRegion.AddStatement($"var {validatorVariableName} = {request.FullName}.{request.ValidationMethod!.Name}();");
						}
					}

					if (request.ValidationMode == ValidationMode.FluentValidation)
					{
						mapContent.AddStatement($"var validationResult = await {validatorVariableName}.ValidateAsync(request);");

						if (parameters.All(p => p.Flag != SourceGenerator.ValidationMode.FluentValidation))
						{
							AddFluentValidation();
							var @if = mapContent.AddIf("!validationResult.IsValid");
							var propertiesWithAttributes = request.Properties.Where(p => p.Attribute != null && p.Property.Name != p.SourceName);
							if (propertiesWithAttributes.Any())
							{
								var scope = @if.AddScope("return Results.ValidationProblem(validationResult.Errors.GroupBy(x =>", ").ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));");
								var @switch = scope.AddScope("return x.PropertyName switch", ";");
								foreach (var property in propertiesWithAttributes)
								{
									@switch.AddStatement($"\"{property.Property.Name}\" => \"{property.SourceName}\",");
								}
								@switch.AddStatement("_ => x.PropertyName");
							}
							else
							{
								@if.AddStatement("return Results.ValidationProblem(validationResult.ToDictionary());");
							}
						}
					}
					else if (request.ValidationMode == ValidationMode.Validot)
					{
						mapContent.AddStatement($"var validationResult = {validatorVariableName}.Validate(request);");
						if (parameters.All(p => p.Flag != SourceGenerator.ValidationMode.Validot))
						{
							var @if = mapContent.AddIf("validationResult.AnyErrors");
							var propertiesWithAttributes = request.Properties.Where(p => p.Attribute != null && p.Property.Name != p.SourceName);
							if (propertiesWithAttributes.Any())
							{
								var scope = @if.AddScope("return Results.ValidationProblem(validationResult.MessageMap.ToDictionary(kvp =>", ", kvp => kvp.Value.ToArray()));");
								var @switch = scope.AddScope("return kvp.Key switch", ";");
								foreach (var property in propertiesWithAttributes)
								{
									@switch.AddStatement($"\"{property.Property.Name}\" => \"{property.SourceName}\",");
								}
								@switch.AddStatement("_ => kvp.Key");
							}
							else
							{
								@if.AddStatement("return Results.ValidationProblem(validationResult.MessageMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()));");
							}
						}
					}
				}
				var typedReturn = endpoint.IsIResult ? "(IResult)" : "TypedResults.Ok";
				if (endpoint.IsStatic)
				{
					mapContent.AddStatement($"return {typedReturn}({awaitCode}{endpointClass.FullName}.{endpoint.HttpMethod}({string.Join(", ", parameters.Select(p => p.Code))}));");
				}
				else
				{
					mapContent.AddStatement($"return {typedReturn}({awaitCode}{endpointClass.InstanceName}.{endpoint.HttpMethod}({string.Join(", ", parameters.Select(p => p.Code))}));");
				}

				mapEndpoints.AddPartialStatement(")");
				if (request?.NeedsValidating ?? false)
				{
					mapEndpoints.AddPartialStatement(".ProducesValidationProblem(400)");
				}
				if (request != null)
				{
					mapEndpoints.AddPartialStatement($".Accepts<{request.FullName}>(\"application/json\")");
				}
				AddOpenApiMetadata(mapEndpoints, endpoint, classesRegion);
				mapEndpoints.AddStatement($"{(endpointClass.HasConfigureMethod ? ")" : "")};");

				counter++;
			}
		}

		servicesMethod.AddStatement($"services.AddTransient<IVoyagerMapping, {generatedNamespace}.EndpointMapper{debugSuffix}>();");

		return source.Build();
	}

	private static void GenerateRequestBodyClasses(RegionBuilder code, RequestObject request)
	{
		if (code.Classes.All(c => c.Name != request.BodyClass))
		{
			var requestBodyClass = code.AddClass(new(request.BodyClass, Access.Private));
			foreach (var bodyProp in request.BodyProperties)
			{
				var prop = requestBodyClass.AddProperty(new($"{bodyProp.Property.Type.ToString().Trim('?')}?", bodyProp.Name));
				foreach (var attr in bodyProp.Property.GetAttributes())
				{
					prop.Attributes.Add(attr.ToString());
				}
			}
		}
	}

	private static void CallValidation(CodeBuilder code, RequestObject request, string variableName)
	{
		foreach (var prop in request.Properties)
		{
			if (prop.Property.NullableAnnotation == NullableAnnotation.NotAnnotated
				&& !prop.Property.Type.IsValueType)
			{
				code.AddStatement($"{variableName}.RuleFor(r => r.{prop.Name}).NotNull();");
			}
		}
		if (request.ValidationMethod != null)
		{
			List<string> parameters = [];
			foreach (var parameter in request.ValidationMethod.Parameters)
			{
				if (parameter.Type.ToDisplayString() == $"FluentValidation.AbstractValidator<{request.FullName}>")
				{
					parameters.Add(variableName);
				}
				else
				{
					var getService = parameter.Type.NullableAnnotation == NullableAnnotation.NotAnnotated ? "GetRequiredService" : "GetService";
					parameters.Add($"app.Services.{getService}<{parameter.Type.ToDisplayString()}>()");
				}
			}
			code.AddStatement($"{request.FullName}.{request.ValidationMethod.Name}({string.Join(", ", parameters)});");
		}
	}


	private void AddOpenApiMetadata(CodeBuilder openApiCode, Endpoint endpoint, RegionBuilder generatedRecords)
	{
		var results = endpoint.FindResults();
		if (results.Count == 0)
		{
			return;
		}
		if (results.Count == 1 && !results[0].IsGeneratedType)
		{
			var result = results[0];
			var typeName = string.IsNullOrWhiteSpace(result.FullTypeName) ? "" : $"<{result.FullTypeName}>";
			openApiCode.AddStatement($".Produces{typeName}({result.StatusCode})");
			return;
		}
		var responseClass = generatedRecords.AddClass(new(endpoint.ResponseName)).AddBase("Voyager.OpenApi.IOneOf");
		for (var i = 0; i < results.Count; ++i)
		{
			var result = results[i];
			if (result.IsGeneratedType)
			{
				responseClass.AddClass(result.ObjectModel, $"Response{i}");
			}
			else
			{
				var typeName = string.IsNullOrWhiteSpace(result.FullTypeName) ? "" : $"<{result.FullTypeName}>";
				responseClass.AddProperty(new($"{result.FullTypeName}", $"Response{i}"));
				openApiCode.AddStatement($".Produces{typeName}({result.StatusCode})");
			}
		}
		openApiCode.AddStatement($".Produces<{endpoint.ResponseName}>(200)");
	}
}
