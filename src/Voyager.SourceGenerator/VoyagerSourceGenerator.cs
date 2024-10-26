﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

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

	public static string GetAssemblyName(this GeneratorExecutionContext context)
	{
		return $"{context.Compilation.AssemblyName?.Replace(".", "_") ?? string.Empty}_VoyagerSourceGen";
	}
}

[Generator]
public class VoyagerSourceGenerator : ISourceGenerator
{
	private const string IResultInterface = "Microsoft.AspNetCore.Http.IResult";

	private readonly ModelBindingSource[] parameterSources = [
		ModelBindingSource.Query,
		ModelBindingSource.Route,
		ModelBindingSource.Header,
		ModelBindingSource.Cookie,
	];

	public void Execute(GeneratorExecutionContext context)
	{
		var code = EndpointMapping(context);
		context.AddSource($"{context.Compilation.AssemblyName}.Voyager.EndpointMapper.g.cs", code);
	}

	public void Initialize(GeneratorInitializationContext context)
	{
	}

	private IEnumerable<EndpointClass> GetEndpointClasses(GeneratorExecutionContext context)
	{
		var treesWithClassesWithAttributes = context.Compilation.SyntaxTrees.Where(
			st => st.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
				.Any(p => p.DescendantNodes().OfType<AttributeSyntax>().Any()));

		foreach (var tree in treesWithClassesWithAttributes)
		{
			var semanticModel = context.Compilation.GetSemanticModel(tree);

			var classesWithAttributes = tree
						.GetRoot()
						.DescendantNodes()
						.OfType<ClassDeclarationSyntax>()
						.Where(cd => cd.DescendantNodes().OfType<AttributeSyntax>().Any());

			foreach (var @class in classesWithAttributes)
			{
				var attribute = @class
					.DescendantNodes()
					.OfType<AttributeSyntax>()
					.FirstOrDefault(a => a.DescendantTokens().Any(dt =>
					{
						if (dt.Parent != null)
						{
							var model = semanticModel.GetTypeInfo(dt.Parent);
							return dt.IsKind(SyntaxKind.IdentifierToken)
								&& $"{model.Type?.ContainingNamespace}.{model.Type?.Name}" == "Voyager.VoyagerEndpointAttribute";
						}
						return false;
					}));
				if (@class is null || attribute is null)
				{
					continue;
				}

				yield return new EndpointClass(@class, semanticModel, attribute);
			}
		}
	}

	private string EndpointMapping(GeneratorExecutionContext context)
	{
		var source = new SourceBuilder();
		source.AddDirective("#nullable enable")
			.AddUsing("FluentValidation")
			.AddUsing("Microsoft.AspNetCore.Builder")
			.AddUsing("Microsoft.AspNetCore.Http.Json")
			.AddUsing("Microsoft.Extensions.DependencyInjection")
			.AddUsing("Microsoft.Extensions.Options")
			.AddUsing("System.Text.Json")
			.AddUsing("Voyager")
			.AddUsing("Voyager.Extensions")
			.AddUsing("Voyager.ModelBinding");

		var voyagerGenNs = source.AddNamespace($"Voyager.Generated.{context.GetAssemblyName()}");
		var servicesMethod = source.AddNamespace("Microsoft.Extensions.DependencyInjection")
			.AddClass(new("VoyagerEndpoints", Access.Internal, isStatic: true))
			.AddMethod(new("AddVoyager", access: Access.Internal, isStatic: true))
			.AddParameter("this IServiceCollection services");
		var endpointMapper = voyagerGenNs
			.AddClass(new("EndpointMapper"));
		var mapEndpoints = endpointMapper
			.AddBase("Voyager.IVoyagerMapping")
			.AddMethod(new("MapEndpoints", access: Access.Public))
			.AddParameter("WebApplication app");
		var endpointsInitRegion = mapEndpoints.AddRegion();
		endpointsInitRegion.AddStatement("var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;");
		endpointsInitRegion.AddStatement("var modelBinder = app.Services.GetService<IModelBinder>() ?? new ModelBinder();");
		endpointsInitRegion.AddStatement("var stringProvider = app.Services.GetService<Voyager.ModelBinding.IStringValuesProvider>() ?? new  Voyager.ModelBinding.StringValuesProvider();");

		var classesRegion = endpointMapper.AddRegion()
			.AddDirective("#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.", "#pragma warning restore CS8618")
			.AddDirective("#pragma warning disable IDE1006 // Naming Styles", "#pragma warning restore IDE1006");

		var validationsAdded = new HashSet<string>();

		foreach (var endpointClass in GetEndpointClasses(context))
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
						GenerateRequestBodyClasses(classesRegion, endpointsInitRegion, request, endpoint);
					}
					else
					{
						AddSchemaId(endpoint, request, endpointsInitRegion);
					}
				}

				if (endpointClass.Configure != null)
				{
					mapEndpoints.AddPartialStatement($"{endpointClass.FullName}.Configure(");
				}
				var minimalApiParams = new[] { "HttpContext context" }.Concat(request?.PropertyValuesFromMinimalApi ?? Enumerable.Empty<string>());
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
				mapEndpoints.AddStatement(").WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => ");
				AddOpenApiMetadata(mapEndpoints, endpoint, request, endpointsInitRegion);

				mapEndpoints.AddStatement($")()){(endpointClass.Configure == null ? "" : ")")};");

				foreach (var response in endpoint.Responses)
				{
					GenerateResponseBodyClasses(classesRegion, response, endpoint, endpointsInitRegion);
				}
			}
		}

		servicesMethod.AddStatement($"services.AddTransient<IVoyagerMapping, Voyager.Generated.{context.GetAssemblyName()}.EndpointMapper>();");

		return source.Build();
	}

	private static void GenerateResponseBodyClasses(RegionBuilder code, ResponseObject response, Endpoint endpoint, CodeBuilder initCode)
	{
		if (code.Classes.All(c => c.Name != response.Name))
		{
			var @class = code.AddClass(new(response.Name, Access.Private));
			foreach (var prop in response.Properties)
			{
				if (prop.Property.IsImplicitlyDeclared)
				{
					continue;
				}
				@class.AddProperty(new($"{prop.Property.Type}", prop.Name));
			}
			AddSchemaId(endpoint, response, initCode, $"Response{(response.Index > 1 ? response.Index : "")}");
		}
	}

	private static void GenerateRequestBodyClasses(RegionBuilder code, CodeBuilder endpointsInitRegion,
		RequestObject request, Endpoint endpoint)
	{
		if (code.Classes.All(c => c.Name != request.BodyClass))
		{
			var requestBodyClass = code.AddClass(new(request.BodyClass, Access.Private));
			foreach (var bodyProp in request.BodyProperties)
			{
				var prop = requestBodyClass.AddProperty(new($"{bodyProp.Property.Type}", bodyProp.Name));
				foreach (var attr in bodyProp.Property.GetAttributes())
				{
					prop.Attributes.Add(attr.ToString());
				}
			}
			AddSchemaId(endpoint, request, endpointsInitRegion);
		}
	}

	private static void AddSchemaId(Endpoint endpoint, RequestObject request, CodeBuilder code)
	{
		var name = PathUtils.ToName(endpoint.Path, "Request", endpoint.HttpMethod);
		code.AddStatement($"Voyager.OpenApi.SchemaIdMapping.Add<{request.BodyClass}>(\"{name}\");");
	}

	private static void AddSchemaId(Endpoint endpoint, ResponseObject response, CodeBuilder code, string? suffix = null)
	{
		var name = PathUtils.ToName(endpoint.Path, suffix ?? string.Empty, endpoint.HttpMethod);
		code.AddStatement($"Voyager.OpenApi.SchemaIdMapping.Add<{response.Name}>(\"{name}\");");
	}

	private static void CallValidation(CodeBuilder code, RequestObject request, string variableName)
	{
		var servicesArg = string.Empty;
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

	private void AddOpenApiMetadata(MethodBuilder mapEndpoints, Endpoint endpoint, RequestObject? request, CodeBuilder initCode)
	{
		var metadata = mapEndpoints.AddScope();
		metadata.AddStatement("var builder = Voyager.OpenApi.OperationBuilderFactory.Create(app.Services, new());");
		if (request != null)
		{
			foreach (var property in request.Properties.Where(p =>
				parameterSources.Contains(p.DataSource)))
			{
				var location = property.DataSource == ModelBindingSource.Route ? "Path" : Enum.GetName(typeof(ModelBindingSource), property.DataSource);
				var required = property.Property.NullableAnnotation == NullableAnnotation.NotAnnotated && !property.Property.Type.IsValueType;
				metadata.AddStatement($"builder.AddParameter(\"{property.SourceName}\", Microsoft.OpenApi.Models.ParameterLocation.{location}, typeof({property.Property.Type.ToString().Trim('?')}), {(required ? "true" : "false")});");
			}
			if (request.HasBody)
			{
				metadata.AddStatement($"builder.AddBody(typeof({request.BodyClass}));");
			}
		}

		metadata.AddStatement("builder.AddResponse(400, typeof(HttpValidationProblemDetails));");
		foreach (var result in endpoint.FindResults())
		{
			metadata.AddStatement($"builder.AddResponse({result.StatusCode}, {(result.Type == null ? "null" : $"typeof({result.Type})")});");
		}
		metadata.AddStatement("return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };");
	}



	public class EndpointConfigureMethod
	{
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
				var propertySybmols = classModel?.GetMembers().Where(m =>
					m.Kind == SymbolKind.Property
					&& m is IPropertySymbol property)
					.OfType<IPropertySymbol>();
				if (propertySybmols != null)
				{
					foreach (var property in propertySybmols)
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
		private readonly List<ObjectProperty> properties = [];
		public IReadOnlyList<ObjectProperty> Properties => properties;
		public IEnumerable<ObjectProperty> BodyProperties => properties.Where(p => p.DataSource == ModelBindingSource.Body);
		public IEnumerable<string> PropertyValuesFromMinimalApi
		{
			get
			{
				return Properties.Select(GetMinimalApiParam).Where(p => p != null)!;
			}
		}

		private string? GetMinimalApiParam(ObjectProperty prop)
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

		private Dictionary<string, (List<AttributeData> Attributes, int? Index, string? Default)> GetParameters(SyntaxNode? declaration, SemanticModel semanticModel)
		{
			var results = new Dictionary<string, (List<AttributeData> Attributes, int? Index, string? Default)>();
			if (declaration is RecordDeclarationSyntax recordDeclaration)
			{
				foreach (var parameter in recordDeclaration.ParameterList?.Parameters ?? [])
				{
					var defaultValue = parameter.Default?.Value.ToString();
					var name = parameter.Identifier.Text;
					var attributes = parameter.AttributeLists.SelectMany(attrList => attrList.Attributes)
						.Select(attr => GetAttributeData(attr, semanticModel)).Where(e => e != null).Select(e => e!).ToList();
					results[name] = (attributes, results.Count, defaultValue);
				}
			}
			return results;
		}

		public static AttributeData? GetAttributeData(AttributeSyntax attributeSyntax, SemanticModel semanticModel)
		{
			// Get the containing symbol (e.g., a class, method, property) where the attribute is applied
			if (attributeSyntax.Parent?.Parent != null)
			{
				var symbol = semanticModel.GetDeclaredSymbol(attributeSyntax.Parent.Parent);

				if (symbol != null)
				{
					// Find the AttributeData matching the attribute syntax
					return symbol.GetAttributes()
						.FirstOrDefault(attr => attr.ApplicationSyntaxReference?.GetSyntax()?.IsEquivalentTo(attributeSyntax) ?? false);
				}
			}
			return null;
		}

		public RequestObject(string requestType, Microsoft.CodeAnalysis.TypeInfo requestTypeInfo,
			string namePrefix, SyntaxNode? declaringSyntax, SemanticModel semanticModel)
		{
			IsRecord = requestTypeInfo.ConvertedType?.IsRecord ?? false;
			var recordParams = GetParameters(declaringSyntax, semanticModel);
			var properties = requestTypeInfo.ConvertedType?.GetMembers().Where(m => m.Kind == SymbolKind.Property) ?? Enumerable.Empty<ISymbol>();
			foreach (var property in properties.OfType<IPropertySymbol>())
			{
				if (property.IsImplicitlyDeclared)
				{
					continue;
				}
				recordParams.TryGetValue(property.Name, out var recordParam);
				var attributes = property.GetAttributes().Concat(recordParam.Attributes ?? []);
				ObjectProperty? requestProperty = null;
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
						requestProperty = new ObjectProperty(property)
						{
							Attribute = attribute,
							DataSource = source,
							SourceAttribute = $"[{attribute}]",
							ConstructorIndex = recordParam.Index
						};
						break;
					}
				}
				requestProperty ??= new ObjectProperty(property)
				{
					DataSource = ModelBindingSource.Body,
					ConstructorIndex = recordParam.Index
				};
				if (recordParam.Default != null)
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

			foreach (var staticMethod in staticMethods ?? Enumerable.Empty<IMethodSymbol>())
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

	internal class ResponseObject
	{
		public ResponseObject(string name, int index)
		{
			Name = $"{name}Response{index}";
			Index = index;
		}

		public ResponseObject(string name)
		{
			Name = name;
		}
		public int Index { get; }
		public string Name { get; }
		public List<ObjectProperty> Properties { get; } = [];

		//var text = $"public {property.Type.ToString()} {objProp.SourceName}";
	}

	internal class ObjectProperty(IPropertySymbol property)
	{
		public int? ConstructorIndex { get; set; } = null;
		public bool IsValueType => Property.Type.IsValueType;
		public string? DefaultValue { get; set; }
		public AttributeData? Attribute { get; set; }
		public ModelBindingSource DataSource { get; set; } = ModelBindingSource.Body;
		public string SourceAttribute { get; set; } = string.Empty;
		public bool IsRequired => Property.IsRequired;
		public IPropertySymbol Property => property;

		public string GetInitValue()
		{
			if (DataSource == ModelBindingSource.Body)
			{
				var defaultValue = DefaultValue ?? (property.NullableAnnotation == NullableAnnotation.Annotated ? "null" : "default");
				return $"body?.{property.Name} ?? {defaultValue}{(property.NullableAnnotation == NullableAnnotation.NotAnnotated ? "!" : "")}";
			}
			else if (DataSource == ModelBindingSource.Route
				|| DataSource == ModelBindingSource.Query
				|| DataSource == ModelBindingSource.Header
				|| DataSource == ModelBindingSource.Form)
			{
				return $"{SourceName}";
			}
			else
			{
				return GetValueFromModelBinder();
			}
		}

		private string GetValueFromModelBinder()
		{
			var parts = Property.Type.ToDisplayParts();
			var typeName = Property.Type.ToDisplayString();
			var isEnumerable = Property.Type.Name == "IEnumerable";
			var suffix = isEnumerable ? "Enumerable" : "";

			if (isEnumerable)
			{
				typeName = parts.Reverse().Skip(1).First().ToString();
			}
			var isNullable = typeName.EndsWith("?");
			var prefix = string.Empty;
			if (isNullable)
			{
				typeName = typeName.Trim('?');
				prefix = "Try";
			}

			var specializationType = typeName switch
			{
				"byte" or "sbyte" or "decimal" or "double" or "float" or "int" or "uint" or "long" or "ulong" or "short" or "ushort" or "char" => "Number",
				"bool" => "Bool",
				"string" => "String",
				_ => "Object",
			};
			var genericType = string.Empty;
			var extraArgs = string.Empty;
			if (specializationType == "Object")
			{
				prefix = string.Empty;
				suffix = string.Empty;
				genericType = $"<{Property.Type.ToDisplayString().Trim('?')}>";
				extraArgs = ", jsonOptions";
			}
			if (specializationType == "Number")
			{
				genericType = $"<{typeName}>";
			}
			var functionName = $"{prefix}Get{specializationType}{suffix}";
			var source = Enum.GetName(typeof(ModelBindingSource), DataSource);
			if (prefix == "Try")
			{
				return $"modelBinder.{functionName}{genericType}(stringProvider.GetStringValues(context, ModelBindingSource.{source}, \"{SourceName}\"){extraArgs}{(DefaultValue == null ? "" : $", {DefaultValue}")}, out var val{Name}) ? val{Name} : default";
			}
			else
			{
				return $"modelBinder.{functionName}{genericType}(stringProvider.GetStringValues(context, ModelBindingSource.{source}, \"{SourceName}\"){extraArgs}{(DefaultValue == null ? "" : $", {DefaultValue}")})";
			}
		}

		public string SourceName
		{
			get
			{
				var propName = $"{char.ToLower(Property.Name[0])}{Property.Name.Substring(1)}";
				if (Attribute != null && Attribute.NamedArguments.Length > 0)
				{
					return Attribute.NamedArguments[0].Value.Value?.ToString() ?? propName;
				}
				return propName;
			}
		}

		public string Name => Property.Name;
	}

	internal class Endpoint
	{
		private readonly MethodDeclarationSyntax method;
		private readonly SemanticModel semanticModel;
		public RequestObject? Request { get; }
		public List<ResponseObject> Responses { get; } = [];

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
				var requestTypeInfo = semanticModel.GetTypeInfo(requestTypeSyntax);
				var declaringSyntax = semanticModel.GetSymbolInfo(name).Symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
				Request = new RequestObject(requestType, requestTypeInfo, NamePrefix, declaringSyntax, semanticModel);
			}
		}

		public bool IsIResult { get; set; } = false;
		public bool IsTask { get; set; } = false;
		public ITypeSymbol? ReturnType { get; set; }
		public string HttpMethod { get; }
		public string Path { get; }
		public string NamePrefix { get; }

		private IEnumerable<(string, string?)> GetResultFromType(ITypeSymbol? type)
		{
			if (type != null && type.ToDisplayString().StartsWith("Microsoft.AspNetCore.Http.HttpResults"))
			{
				if (type is INamedTypeSymbol namedSymbol && namedSymbol.TypeArguments.Any())
				{
					return new (string, string?)[] { ($"TypedResults.{type.Name}().StatusCode", namedSymbol.TypeArguments[0].ToDisplayString()) }; ;
				}
				return new (string, string?)[] { ($"TypedResults.{type.Name}().StatusCode", null) };
			}
			return Enumerable.Empty<(string, string?)>();
		}

		private IEnumerable<(string, string?)> GetResultFromExpression(ExpressionSyntax? expression)
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
				if (model.Symbol is ILocalSymbol localSymbol)
				{
					var type = localSymbol.Type;
					var typeResults = GetResultFromType(type);
					if (typeResults.Any())
					{
						return typeResults;
					}
				}
				else if (model.Symbol is IMethodSymbol methodSymbol)
				{
					if (methodSymbol.ReceiverType?.ToDisplayString() == "Microsoft.AspNetCore.Http.TypedResults")
					{
						var type = methodSymbol.IsGenericMethod ? methodSymbol.TypeArguments[0] : null;
						if (type?.IsAnonymousType ?? false)
						{
							var responseObj = AddResponseObject(type);
							return [($"TypedResults.{methodSymbol.Name}().StatusCode", responseObj.Name)];
						}
						else
						{
							return [($"TypedResults.{methodSymbol.Name}().StatusCode", type?.ToDisplayString())];
						}
					}
				}
			}
			return Enumerable.Empty<(string, string?)>();
		}

		private ResponseObject AddResponseObject(ITypeSymbol typeSymbol)
		{
			var responseObj = new ResponseObject(NamePrefix, Responses.Count + 1);
			foreach (var member in typeSymbol.GetMembers())
			{
				if (member is IPropertySymbol property)
				{
					responseObj.Properties.Add(new(property));
				}
			}
			Responses.Add(responseObj);
			return responseObj;
		}

		public List<(string StatusCode, string? Type)> FindResultsInNodes(IEnumerable<SyntaxNode> nodes, bool allowLambda)
		{
			List<(string StatusCode, string? Type)> results = [];
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

		public List<(string StatusCode, string? Type)> FindResults()
		{
			var results = new List<(string, string?)>();

			if (!IsIResult)
			{
				if (ReturnType != null && !ReturnType.IsValueType)
				{
					var responseObject = AddResponseObject(ReturnType);
					results.Add(("200", responseObject.Name));
				}
				else
				{
					results.Add(("200", ReturnType?.ToDisplayString()));
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