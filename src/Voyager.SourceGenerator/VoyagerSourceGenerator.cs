using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using static Voyager.SourceGenerator.SourceEmitter;

namespace Voyager.SourceGenerator;

public enum ModelBindingSource
{
	Route,
	Query,
	Cookie,
	Header,
	Form,
	Body
}

public enum InstanceInfoFlags
{
	None,
	FluentValidationResult,
	ValidotResult
}

public class InstanceInfo(string code)
{
	public string Code => code;

	public InstanceInfoFlags Flag { get; set; } = InstanceInfoFlags.None;

	public override string ToString()
	{
		return Code;
	}
}

public static class Extension
{
	public static InstanceInfo? GetInstanceOf(this ITypeSymbol? type)
	{
		if (type is null)
		{
			return null;
		}
		var typeName = type.ToDisplayString().Trim('?');
		if (typeName == "Microsoft.AspNetCore.Http.HttpContext")
		{
			return new InstanceInfo("context");
		}
		else if (typeName == "System.Threading.CancellationToken")
		{
			return new InstanceInfo("context.RequestAborted");
		}
		else if (typeName == "FluentValidation.Results.ValidationResult")
		{
			return new InstanceInfo("validationResult") { Flag = InstanceInfoFlags.FluentValidationResult };
		}
		else if (typeName == "Validot.Results.IValidationResult")
		{
			return new InstanceInfo("validationResult") { Flag = InstanceInfoFlags.ValidotResult };
		}
		else
		{
			return new InstanceInfo($"context.RequestServices.GetRequiredService<{typeName}>()");
		}
	}

	public static string? GetValidationInstanceOf(this ITypeSymbol? type)
	{
		if (type is null)
		{
			return null;
		}
		var typeName = type.ToDisplayString().Trim('?');
		return $"app.Services.GetRequiredService<{typeName}>();";
	}

	public static string GetAssemblyName(this Compilation compilation)
	{
		return $"{compilation.AssemblyName?.Replace(".", "_") ?? string.Empty}_VoyagerSourceGen";
	}
}

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
	private const string IResultInterface = "Microsoft.AspNetCore.Http.IResult";


	private readonly ModelBindingSource[] parameterSources = [
		ModelBindingSource.Query,
		ModelBindingSource.Route,
		ModelBindingSource.Header,
		ModelBindingSource.Cookie,
	];

	internal string Emit(IEnumerable<EndpointClass> endpointClasses, Compilation compilation, string debugSuffix)
	{
		var source = new SourceBuilder();
		source.AddDirective("#nullable enable annotations")
			.AddUsing("FluentValidation")
			.AddUsing("Microsoft.AspNetCore.Builder")
			.AddUsing("Microsoft.AspNetCore.Http.Json")
			.AddUsing("Microsoft.Extensions.DependencyInjection")
			.AddUsing("Microsoft.Extensions.Options")
			.AddUsing("System.Text.Json")
			.AddUsing("Voyager")
			.AddUsing("Voyager.Extensions")
			.AddUsing("Voyager.ModelBinding")
			.AddUsing("Voyager.OpenApi")
			.AddUsing("Microsoft.OpenApi.Models")
			.AddUsing("System.ComponentModel.DataAnnotations")
			.AddUsing("Microsoft.Extensions.DependencyInjection.Extensions");

		var voyagerGenNs = source.AddNamespace($"Voyager.Generated.{compilation.AssemblyName}");
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
			.AddParameter("WebApplication app")
			.AddParameter("ISchemaIdGenerator schemaIdGenerator");
		var componentsCode = endpointMapper
			.AddMethod(new("GetOpenApiComponents", "IDictionary<Type, OpenApiSchema>", Access.Public))
			.AddParameter("ISchemaIdGenerator schemaIdGenerator");
		var componentTypesCode = endpointMapper
			.AddMethod(new("GetComponentTypes", "IEnumerable<Type>", Access.Public));
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

				if (endpointClass.Configure != null)
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
					var variableName = request.NeedsBodyClassGenerated ? "body" : "request";
					mapContent.AddStatement($"var {variableName} = await JsonSerializer.DeserializeAsync<{request.BodyClass}>(context.Request.Body, jsonOptions);");
					if (!request.NeedsBodyClassGenerated)
					{
						var @if = mapContent.AddIf("request == null");
						@if.AddStatement("return TypedResults.Problem(\"Unable to parse request body\", statusCode: 400);");
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
							endpointsInitRegion.AddStatement($"var {validatorVariableName} = new Voyager.Validation.GenericValidator<{request.FullName}>();");
							CallValidation(endpointsInitRegion, request, validatorVariableName);
						}
						else if (request.ValidationMode == ValidationMode.Validot)
						{
							endpointsInitRegion.AddStatement($"var {validatorVariableName} = {request.FullName}.{request.ValidationMethod!.Name}();");
						}
					}

					if (request.ValidationMode == ValidationMode.FluentValidation)
					{
						mapContent.AddStatement($"var validationResult = await {validatorVariableName}.ValidateAsync(request);");

						if (parameters.All(p => p.Flag != InstanceInfoFlags.FluentValidationResult))
						{
							var @if = mapContent.AddIf("!validationResult.IsValid");
							var propertiesWithAttributes = request.Properties.Where(p => p.Attribute != null && p.Property.Name != p.SourceName);
							if (propertiesWithAttributes.Any())
							{
								@if.AddStatement("var dictionary = validationResult.ToDictionary();");
								foreach (var property in propertiesWithAttributes)
								{
									@if.AddStatement($"dictionary.ReplaceKey(\"{property.Property.Name}\", \"{property.SourceName}\");");
								}
								@if.AddStatement("return Results.ValidationProblem(dictionary);");
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
						if (parameters.All(p => p.Flag != InstanceInfoFlags.ValidotResult))
						{
							var @if = mapContent.AddIf("validationResult.AnyErrors");
							@if.AddStatement("var dictionary = validationResult.MessageMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());");
							var propertiesWithAttributes = request.Properties.Where(p => p.Attribute != null && p.Property.Name != p.SourceName);
							if (propertiesWithAttributes.Any())
							{
								foreach (var property in propertiesWithAttributes)
								{
									@if.AddStatement($"dictionary.ReplaceKey(\"{property.Property.Name}\", \"{property.SourceName}\");");
								}
							}
							@if.AddStatement("return Results.ValidationProblem(dictionary);");
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
				AddOpenApiMetadata2(mapEndpoints, endpoint, classesRegion);
				mapEndpoints.AddStatement($"{(endpointClass.Configure == null ? "" : ")")};");

				counter++;
			}
		}
		var httpValidationProblemsSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpValidationProblemDetails");
		if (httpValidationProblemsSymbol != null)
		{
			schemaGenerator.ReferencedTypes.Add(httpValidationProblemsSymbol);
		}

		var componentsScope = componentsCode.AddScope("return new Dictionary<Type, OpenApiSchema>()", ";");
		var componentTypesScope = componentTypesCode.AddScope("return [", "];", ScopeType.None);

		if (schemaGenerator.ReferencedTypes.Count > 0)
		{

			var generatedReferences = new HashSet<string>();
			for (var i = 0; i < schemaGenerator.ReferencedTypes.Count; ++i)
			{
				var typeName = schemaGenerator.ReferencedTypes[i].OriginalDefinition.ToDisplayString();
				if (!generatedReferences.Contains(typeName))
				{
					var scope = componentsScope.AddScope($"[typeof({typeName})] =", ",", ScopeType.None);
					schemaGenerator.GenerateOpenApiSchemaCode(schemaGenerator.ReferencedTypes[i], scope);
					generatedReferences.Add(typeName);
					componentTypesScope.AddStatement($"typeof({typeName}),");
				}
			}
		}

		servicesMethod.AddStatement($"services.AddTransient<IVoyagerMapping, Voyager.Generated.{compilation.AssemblyName}.EndpointMapper{debugSuffix}>();");

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

	private readonly OpenApiSchemaGenerator schemaGenerator = new();

	private void AddOpenApiMetadata2(CodeBuilder openApiCode, Endpoint endpoint, RegionBuilder generatedRecords)
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

	public class EndpointConfigureMethod
	{
	}

	internal enum ValidationMode
	{
		FluentValidation,
		Validot
	}

	internal class RequestObject
	{
		public IMethodSymbol? ValidationMethod { get; set; }
		public bool NeedsValidating => ValidationMethod != null || properties.Any(p => p.IsRequired);
		public ValidationMode ValidationMode { get; private set; } = ValidationMode.FluentValidation;
		private readonly List<PropertyModel> properties = [];
		public IReadOnlyList<PropertyModel> Properties => properties;
		public IEnumerable<PropertyModel> BodyProperties => properties.Where(p => p.DataSource == ModelBindingSource.Body);
		public IEnumerable<string> PropertyValuesFromMinimalApi
		{
			get
			{
				return Properties.Select(GetMinimalApiParam).Where(p => p != null)!;
			}
		}

		private string? GetMinimalApiParam(PropertyModel prop)
		{
			var attr = (prop.DataSource != ModelBindingSource.Cookie && prop.DataSource != ModelBindingSource.Body)
				? prop.SourceAttribute : null;
			if (attr == null)
			{
				return null;
			}
			var value = $"{attr.Replace("Attribute", "")}{prop.Property.Type.ToDisplayString()} {prop.SourceName}";
			if (prop.DefaultValue != null)
			{
				value += $" = {prop.DefaultValue}";
			}
			return value;
		}

		public bool HasBody => properties.Any(p => p.DataSource == ModelBindingSource.Body);
		public bool NeedsBodyClassGenerated => Properties.Any(p => p.DataSource != ModelBindingSource.Body);
		public string BodyClass => NeedsBodyClassGenerated ? $"{Name}RequestBody" : FullName;
		public string Name { get; }
		public string FullName { get; }
		public bool IsRecord { get; }
		public ITypeSymbol? TypeSymbol { get; }

		public RequestObject(Microsoft.CodeAnalysis.TypeInfo requestTypeInfo,
			string namePrefix, SyntaxNode? declaringSyntax, SemanticModel semanticModel)
		{
			IsRecord = requestTypeInfo.ConvertedType?.IsRecord ?? false;
			TypeSymbol = requestTypeInfo.Type;

			var recordParams = TypeHelper.GetRecordParameters(declaringSyntax, semanticModel);
			var properties = requestTypeInfo.ConvertedType?.GetMembers().Where(m => m.Kind == SymbolKind.Property) ?? [];
			foreach (var property in properties.OfType<IPropertySymbol>())
			{
				if (property.IsImplicitlyDeclared)
				{
					continue;
				}
				recordParams.TryGetValue(property.Name, out var recordParam);
				var attributes = property.GetAttributes().Concat(recordParam?.Attributes ?? []);
				PropertyModel? requestProperty = null;
				foreach (var attribute in attributes)
				{
					var source = attribute.AttributeClass?.Name switch
					{
						"FromQueryAttribute" => ModelBindingSource.Query,
						"FromRouteAttribute" => ModelBindingSource.Route,
						"FromFormAttribute" => ModelBindingSource.Form,
						"FromHeaderAttribute" => ModelBindingSource.Header,
						"FromCookieAttribute" => ModelBindingSource.Cookie,
						_ => ModelBindingSource.Body
					};
					if (source != ModelBindingSource.Body)
					{
						requestProperty = new PropertyModel(property)
						{
							Attribute = attribute,
							DataSource = source,
							SourceAttribute = $"[{attribute}]",
							ConstructorIndex = recordParam?.Index
						};
						break;
					}
				}
				requestProperty ??= new PropertyModel(property)
				{
					DataSource = ModelBindingSource.Body,
					ConstructorIndex = recordParam?.Index
				};
				if (recordParam?.Default != null)
				{
					requestProperty.DefaultValue = recordParam.Default;
				}
				else if (property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is PropertyDeclarationSyntax syntax
						&& syntax.Initializer != null)
				{
					requestProperty.DefaultValue = syntax.Initializer.Value.ToString();
				}
				this.properties.Add(requestProperty);
			}

			Name = namePrefix;
			FullName = requestTypeInfo.Type?.OriginalDefinition?.ToString() ?? Name;


			var staticMethods = requestTypeInfo.ConvertedType?.GetMembers().Where(m => m.Kind == SymbolKind.Method
				&& m.IsStatic).OfType<IMethodSymbol>();

			foreach (var staticMethod in staticMethods ?? [])
			{
				var parameterTypes = staticMethod.Parameters.ToList();
				if (parameterTypes.Any(p => p.Type.ToDisplayString() == $"FluentValidation.AbstractValidator<{requestTypeInfo.Type?.ToDisplayString()}>"))
				{
					ValidationMethod = staticMethod;
					break;
				}
				if (staticMethod.ReturnType.ToDisplayString().Contains($"Validot.IValidator<{FullName}>"))
				{
					ValidationMode = ValidationMode.Validot;
					ValidationMethod = staticMethod;
				}
			}
		}
	}

	internal class Endpoint
	{
		private readonly MethodDeclarationSyntax method;
		private readonly SemanticModel semanticModel;
		public RequestObject? Request { get; }

		public bool NeedsAsync => IsTask || Request != null;
		private readonly string[] requestNames = ["request", "req"];
		public bool IsStatic => method.Modifiers.Any(SyntaxKind.StaticKeyword);
		public IEnumerable<InstanceInfo> GetInjectedParameters()
		{
			return method.ParameterList.Parameters
				.Select(p =>
				{
					if (requestNames.Any(rn => rn.Equals(p.Identifier.ValueText, StringComparison.Ordinal)))
					{
						return new InstanceInfo("request");
					}
					return semanticModel.GetTypeInfo(p.Type!).Type.GetInstanceOf();
				}).Where(p => p != null)!;
		}

		public Endpoint(MethodDeclarationSyntax method, SemanticModel semanticModel, string httpMethod, string namePrefix, string path)
		{
			ReturnType = semanticModel.GetTypeInfo(method.ReturnType).Type;
			if (ReturnType is INamedTypeSymbol namedSymbol &&
				(ReturnType?.Name == "Task" || ReturnType?.Name == "ValueTask"))
			{
				IsTask = true;
				ReturnType = namedSymbol.TypeArguments[0];
			}
			if (ReturnType?.ToDisplayString() == IResultInterface
				|| (ReturnType?.AllInterfaces.Select(i => i.ToDisplayString()).Contains(IResultInterface) ?? false))
			{
				IsIResult = true;
			}

			this.method = method;
			this.semanticModel = semanticModel;
			HttpMethod = httpMethod;
			Path = path.Trim('"');
			NamePrefix = $"{namePrefix}{HttpMethod}";
			var requestTypeSyntax = method.ParameterList.Parameters.FirstOrDefault(p => requestNames.Any(rn => rn.Equals(p.Identifier.Text, StringComparison.OrdinalIgnoreCase)))?.Type;
			var requestType = string.Empty;
			if (requestTypeSyntax is IdentifierNameSyntax name)
			{
				requestType = name.Identifier.ToFullString().Trim();
				var requestTypeInfo = semanticModel.GetTypeInfo(name);
				var declaringSyntax = semanticModel.GetSymbolInfo(name).Symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
				Request = new RequestObject(requestTypeInfo, NamePrefix, declaringSyntax, semanticModel);
			}
		}

		public bool IsIResult { get; set; } = false;
		public bool IsTask { get; set; } = false;
		public ITypeSymbol? ReturnType { get; set; }
		public string HttpMethod { get; }
		public string Path { get; }
		public string ResponseName => PathUtils.ToName(Path, "Response", HttpMethod);
		public string NamePrefix { get; }

		private IEnumerable<MethodResult> GetResultFromExpression(ExpressionSyntax? expression)
		{
			if (expression != null)
			{
				if (expression is ConditionalExpressionSyntax conditional)
				{
					return GetResultFromExpression(conditional.WhenTrue).Concat(
						GetResultFromExpression(conditional.WhenFalse));
				}
				else if (expression is AwaitExpressionSyntax awaitSyntax)
				{
					return FindResultsInNodes(awaitSyntax.DescendantNodes(), true);
				}
				else if (expression is CastExpressionSyntax castExpression)
				{
					return GetResultFromExpression(castExpression.Expression);
				}
				else if (expression is ParenthesizedExpressionSyntax parenSyntax)
				{
					return GetResultFromExpression(parenSyntax.Expression);
				}
				var model = semanticModel.GetSymbolInfo(expression);
				var result = MethodResult.Create(model.Symbol, semanticModel);
				if (result != null)
				{
					return [result];
				}
			}
			return [];
		}

		public List<MethodResult> FindResultsInNodes(IEnumerable<SyntaxNode> nodes, bool allowLambda)
		{
			List<MethodResult> results = [];
			var returns = nodes.OfType<ReturnStatementSyntax>()
					.Where(rs => allowLambda || !rs.AncestorsAndSelf().OfType<LambdaExpressionSyntax>().Any() &&
						!rs.AncestorsAndSelf().OfType<LocalFunctionStatementSyntax>().Any())
					.Where(n => n.IsKind(SyntaxKind.ReturnStatement)).ToList();
			foreach (var statement in returns)
			{
				var expressionResults = GetResultFromExpression(statement.Expression);
				results.AddRange(expressionResults);
			}
			return results;
		}

		public List<MethodResult> FindResults()
		{
			var results = new List<MethodResult>();

			if (!IsIResult)
			{
				var result = MethodResult.Create(ReturnType, semanticModel);
				if (result != null)
				{
					results.Add(result);
				}
				return results;
			}

			if (method.Body != null)
			{
				results.AddRange(FindResultsInNodes(method.Body.DescendantNodes(), false));
			}
			return results;
		}
	}
}

public class OpenApiSchemaGenerator
{
	public void GenerateOpenApiSchemaCode(ITypeSymbol typeSymbol, CodeBuilder codeBuilder, IEnumerable<string>? excludedProperties = null)
	{
		excludedProperties ??= [];
		if (typeSymbol.IsSimpleType())
		{
			var (code, _) = GenerateSchemaForType(typeSymbol);
			codeBuilder.AddStatement(code);
			return;
		}
		var schemaName = $"{typeSymbol.Name}Schema";

		var scope = codeBuilder.AddScope("new OpenApiSchema");
		scope.AddStatement("Type = \"object\",");
		var properties = scope.AddScope("Properties = new Dictionary<string, OpenApiSchema>()", ",");
		var required = new List<string>();
		foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
		{
			if (member.IsImplicitlyDeclared || excludedProperties.Contains(member.Name))
			{
				continue;
			}
			var memberScope = properties.AddScope("", ",");
			memberScope.AddStatement($"\"{member.Name}\",");
			if (GeneratePropertySchema(member, memberScope))
			{
				required.Add(member.Name);
			}
		}
		if (required.Any())
		{
			scope.AddStatement($"Required = new HashSet<string>{{{string.Join(", ", required.Select(p => $"\"{p}\""))}}}");
		}
	}

	private bool GeneratePropertySchema(IPropertySymbol property, CodeBuilder codeBuilder)
	{
		var type = property.Type;
		// Handle primitive or complex types
		var (code, required) = GenerateSchemaForType(type);
		codeBuilder.AddStatement(code);
		return required;
	}

	private (string Code, bool Required) GenerateSchemaForType(ITypeSymbol? typeSymbol)
	{
		if (typeSymbol == null)
		{
			return (string.Empty, false);
		}
		var nullable = typeSymbol.IsNullable();
		typeSymbol = typeSymbol.TryGetUnderlyingType();

		if (typeSymbol.TryGetDictionaryValueType(out var valueType))
		{
			return ($"new OpenApiSchema {{ Type = \"object\", AdditionalProperties = {GenerateSchemaForType(valueType).Code}{GetNullableProp(nullable)} }}", !nullable);
		}
		if (typeSymbol.TryGetEnumerableElementType(out var elementType))
		{
			return ($"new OpenApiSchema {{ Type = \"array\", Items = {GenerateSchemaForType(elementType).Code}{GetNullableProp(nullable)} }}", !nullable);
		}

		return (typeSymbol.SpecialType switch
		{
			SpecialType.System_String or SpecialType.System_Char => GenSimpleSchema("string", nullable),
			SpecialType.System_Int32 => GenSimpleSchema("integer", nullable, "int32"),
			SpecialType.System_Boolean => GenSimpleSchema("boolean", nullable),
			SpecialType.System_Double or SpecialType.System_Decimal => GenSimpleSchema("number", nullable, "double"),
			SpecialType.System_Int64 => GenSimpleSchema("integer", nullable, "int64"),
			SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16
				or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64
				or SpecialType.System_Single => GenSimpleSchema("number", nullable),
			SpecialType.System_DateTime => GenSimpleSchema("string", nullable, "date-time"),
			_ => GenerateSchemaForObject(typeSymbol, nullable)
		}, !nullable);
	}

	private static string GetNullableProp(bool nullable)
	{
		return nullable ? ", Nullable = true" : "";
	}

	private string GenSimpleSchema(string type, bool nullable, string? format = null)
	{
		return $"new OpenApiSchema {{ Type = \"{type}\"{(format == null ? "" : $", Format = \"{format}\"")}{GetNullableProp(nullable)} }}";
	}

	private string GenerateSchemaForObject(ITypeSymbol typeSymbol, bool nullable)
	{
		var typeName = typeSymbol.OriginalDefinition.ToString();
		if (typeName == typeof(DateTimeOffset).FullName)
		{
			return GenSimpleSchema("string", nullable, "date-time");
		}
		else if (typeName == typeof(TimeSpan).FullName)
		{
			return GenSimpleSchema("string", nullable, "date-span");
		}
		else if (typeName == typeof(Guid).FullName)
		{
			return GenSimpleSchema("string", nullable, "uuid");
		}
		else if (typeName == typeof(Uri).FullName)
		{
			return GenSimpleSchema("string", nullable);
		}
		else if (typeName == typeof(object).FullName)
		{
			return GenSimpleSchema("object", nullable);
		}
		else
		{
			if (typeSymbol is INamedTypeSymbol namedType
				&& namedType.IsGenericType)
			{
				return GenSimpleSchema("object", nullable);
			}
			ReferencedTypes.Add(typeSymbol);
			return $"new OpenApiSchema {{ Reference = new OpenApiReference {{ Id = schemaIdGenerator.GetId<{typeSymbol.OriginalDefinition}>(), Type = ReferenceType.Schema }}, }}";
		}
	}

	public List<ITypeSymbol> ReferencedTypes { get; } = [];
}

public static class TypeSymbolExtensions
{
	public static bool IsNullable(this ITypeSymbol typeSymbol)
	{
		return typeSymbol.NullableAnnotation != NullableAnnotation.NotAnnotated;
	}

	public static ITypeSymbol TryGetUnderlyingType(this ITypeSymbol typeSymbol)
	{
		// Check if the type symbol is a nullable value type
		if (typeSymbol is INamedTypeSymbol namedType &&
			namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
		{
			// Return the underlying type (the first type argument of Nullable<T>)
			return namedType.TypeArguments[0];
		}

		// Return null if it's not a nullable value type
		return typeSymbol;
	}

	public static bool TryGetDictionaryValueType(this ITypeSymbol typeSymbol, out ITypeSymbol? elementType)
	{
		if (typeSymbol is INamedTypeSymbol namedSymbol
			&& namedSymbol.OriginalDefinition.ToString() == "System.Collections.Generic.IDictionary<TKey, TValue>"
			&& namedSymbol.TypeArguments.Length == 2)
		{
			elementType = namedSymbol.TypeArguments[1];
			return true;
		}

		// Check if the type implements IEnumerable<T> and extract T
		var ienumerableType = typeSymbol
			.AllInterfaces
			.FirstOrDefault(i => i.OriginalDefinition.ToString() == "System.Collections.Generic.IDictionary<TKey, TValue>");

		if (ienumerableType != null && ienumerableType.TypeArguments.Length == 2)
		{
			elementType = ienumerableType.TypeArguments[1];
			return true;
		}
		elementType = null;
		return false;
	}

	public static bool TryGetEnumerableElementType(this ITypeSymbol typeSymbol, out ITypeSymbol? elementType)
	{
		if (typeSymbol.SpecialType == SpecialType.System_String)
		{
			elementType = null;
			return false;
		}
		// Check if the type is an array
		if (typeSymbol is IArrayTypeSymbol arrayType)
		{
			elementType = arrayType.ElementType;
			return true;
		}

		if (typeSymbol is INamedTypeSymbol namedSymbol
			&& namedSymbol.OriginalDefinition.ToString() == "System.Collections.Generic.IEnumerable<T>"
			&& namedSymbol.TypeArguments.Length == 1)
		{
			elementType = namedSymbol.TypeArguments[0];
			return true;
		}

		// Check if the type implements IEnumerable<T> and extract T
		var ienumerableType = typeSymbol
			.AllInterfaces
			.FirstOrDefault(i => i.OriginalDefinition.ToString() == "System.Collections.Generic.IEnumerable<T>");

		if (ienumerableType != null && ienumerableType.TypeArguments.Length == 1)
		{
			elementType = ienumerableType.TypeArguments[0];
			return true;
		}

		// Check if the type implements non-generic IEnumerable
		if (typeSymbol.AllInterfaces.Any(i => i.ToString() == "System.Collections.IEnumerable"))
		{
			elementType = null; // No specific element type for non-generic IEnumerable
			return true;
		}

		// Not an enumerable type
		elementType = null;
		return false;
	}

	public static bool IsSimpleType(this ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
		{
			return false;
		}

		switch (typeSymbol.SpecialType)
		{
			case SpecialType.System_Boolean:
			case SpecialType.System_Byte:
			case SpecialType.System_SByte:
			case SpecialType.System_Int16:
			case SpecialType.System_UInt16:
			case SpecialType.System_Int32:
			case SpecialType.System_UInt32:
			case SpecialType.System_Int64:
			case SpecialType.System_UInt64:
			case SpecialType.System_Single:
			case SpecialType.System_Double:
			case SpecialType.System_Decimal:
			case SpecialType.System_Char:
			case SpecialType.System_String:
			case SpecialType.System_DateTime:
				return true;
		}

		var typeName = typeSymbol.OriginalDefinition.ToString();
		if (typeName == typeof(DateTimeOffset).FullName
			|| typeName == typeof(TimeSpan).FullName
			|| typeName == typeof(Guid).FullName
			|| typeName == typeof(Uri).FullName
			|| typeName == typeof(object).FullName)
		{
			return true;
		}
		return false;
	}
}

internal class EndpointClass
{
	private readonly ClassDeclarationSyntax syntax;
	private readonly string[] httpMethods = ["Get", "Put", "Post", "Delete", "Patch"];
	private readonly List<Endpoint> endpointMethods = [];
	public IReadOnlyList<Endpoint> EndpointMethods => endpointMethods;
	public EndpointConfigureMethod? Configure { get; }
	public string FullName { get; }
	public string InstanceName => $"inst_{FullName.Replace(".", "_")}";
	private readonly INamedTypeSymbol? classModel;
	public string Path { get; }
	public bool CanBeSingleton { get; }
	private readonly List<PropertyInfo> properties = [];
	public string Namespace => classModel?.ContainingNamespace.Name ?? string.Empty;
	public bool IsStatic => (classModel?.IsStatic ?? false) || endpointMethods.All(m => m.IsStatic);

	public EndpointClass(ClassDeclarationSyntax syntax, SemanticModel semanticModel, AttributeSyntax attribute)
	{
		var pathToken = attribute.ArgumentList?.Arguments[0].DescendantTokens().First();
		Path = pathToken.ToString() ?? string.Empty;
		classModel = semanticModel.GetDeclaredSymbol(syntax);
		FullName = classModel?.OriginalDefinition.ToString() ?? string.Empty;
		this.syntax = syntax;
		foreach (var (methodSyntax, httpMethod) in syntax.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>()
				.Select(m => (m, httpMethods.FirstOrDefault(httpMethod => m.Identifier.ToString().Equals(httpMethod, StringComparison.OrdinalIgnoreCase))))
				.Where((tuple) => tuple.Item2 != null))
		{
			endpointMethods.Add(new Endpoint(methodSyntax, semanticModel, httpMethod, FullName.Replace(".", "_"), Path));
		}
		var configureSyntax = syntax.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>()
					.Where(m => m.Identifier.ToString() == "Configure").FirstOrDefault();
		if (configureSyntax != null)
		{
			Configure = new EndpointConfigureMethod();
		}
		FindProperties();
		CanBeSingleton = (!classModel?.IsStatic ?? false) && DetermineIfCanBeSingleton();
	}

	bool DetermineIfCanBeSingleton()
	{
		if (syntax.Members.Any(m => m.IsKind(SyntaxKind.FieldDeclaration))
			|| properties.Any())
		{
			return false;
		}
		return true;
	}

	private void FindProperties()
	{
		if (classModel != null)
		{
			var propertySymbols = classModel?.GetMembers().Where(m =>
				m.Kind == SymbolKind.Property
				&& m is IPropertySymbol property)
				.OfType<IPropertySymbol>();
			if (propertySymbols != null)
			{
				foreach (var property in propertySymbols)
				{
					var shouldInject = property.IsRequired ||
						property.GetAttributes().Any(attr => attr.AttributeClass?.ToString() == "Microsoft.AspNetCore.Mvc.FromServicesAttribute");
					properties.Add(new(property) { Injected = shouldInject });
				}
			}
		}
	}

	public IEnumerable<IPropertySymbol> GetPropertiesNeedingInjected()
	{
		return properties.Where(p => p.Injected).Select(p => p.PropertySymbol);
	}

	private class PropertyInfo(IPropertySymbol propertySymbol)
	{
		public IPropertySymbol PropertySymbol { get; set; } = propertySymbol;
		public bool Injected { get; set; } = false;
	}
}

internal class MethodResult
{
	private MethodResult(string statusCode, ObjectModel objectModel)
	{
		StatusCode = statusCode;
		ObjectModel = objectModel;
	}
	public string? FullTypeName { get; set; }
	public string StatusCode { get; private set; }
	public ObjectModel ObjectModel { get; }
	public ITypeSymbol? Symbol { get; set; }
	public bool IsGeneratedType { get; set; } = false;

	public static MethodResult? Create(ISymbol? symbol, SemanticModel semanticModel, string? statusCode = null)
	{
		if (symbol is ILocalSymbol localSymbol)
		{
			var type = localSymbol.Type;
			var result = Create(type, semanticModel, statusCode);
			if (result != null)
			{
				return result;
			}
		}
		else if (symbol is IMethodSymbol methodSymbol)
		{
			if (methodSymbol.ReceiverType?.ToDisplayString() == "Microsoft.AspNetCore.Http.TypedResults")
			{
				var type = methodSymbol.IsGenericMethod ? methodSymbol.TypeArguments[0] : null;
				var finalType = (type?.IsAnonymousType ?? false) ? methodSymbol.ReceiverType : type;
				return Create(type, semanticModel, $"TypedResults.{methodSymbol.Name}().StatusCode");
			}
		}
		else if (symbol is ITypeSymbol typeSymbol)
		{
			if (typeSymbol != null && typeSymbol.ToDisplayString().StartsWith("Microsoft.AspNetCore.Http.HttpResults"))
			{
				if (typeSymbol is INamedTypeSymbol namedSymbol && namedSymbol.TypeArguments.Any())
				{
					return new($"TypedResults.{typeSymbol.Name}().StatusCode.ToString()", new(typeSymbol, semanticModel))
					{
						FullTypeName = namedSymbol.TypeArguments[0].ToDisplayString(),
						Symbol = typeSymbol
					};
				}
				return new($"TypedResults.{typeSymbol.Name}().StatusCode.ToString()", new(typeSymbol, semanticModel)) { Symbol = typeSymbol };
			}
			else if (typeSymbol != null)
			{
				return new(statusCode ?? "200", new(typeSymbol, semanticModel))
				{
					FullTypeName = typeSymbol.ToDisplayString(),
					Symbol = typeSymbol,
					IsGeneratedType = typeSymbol.IsAnonymousType
				};
			}
		}
		return null;
	}
}
