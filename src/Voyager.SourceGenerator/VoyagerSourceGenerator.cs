using Microsoft.CodeAnalysis;
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

public static class Extension
{
	public static string? GetInstanceOf(this ITypeSymbol? type)
	{
		if (type is null)
		{
			return null;
		}
		var typeName = type.ToDisplayString().Trim('?');
		if (typeName == "Microsoft.AspNetCore.Http.HttpContext")
		{
			return "context";
		}
		else if (typeName == "System.Threading.CancellationToken")
		{
			return "context.RequestAborted";
		}
		else if (typeName == "FluentValidation.Results.ValidationResult")
		{
			return "validationResult";
		}
		else
		{
			return $"context.RequestServices.GetRequiredService<{typeName}>()";
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
			.AddUsing("Voyager.ModelBinding");

		var voyagerGenNs = source.AddNamespace($"Voyager.Generated.{context.Compilation.AssemblyName}Gen");
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
		endpointsInitRegion.AddStatement("var modelBinder = app.Services.GetService<IModelBinder>() ?? new ModelBinder();");
		endpointsInitRegion.AddStatement("var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;");

		foreach (var endpointClass in GetEndpointClasses(context))
		{
			foreach (var endpoint in endpointClass.EndpointMethods)
			{
				var request = endpoint.Request;

				if (request != null)
				{
					GenerateRequestBodyClasses(endpointMapper, endpointsInitRegion, request);
				}

				servicesMethod.AddStatement($"services.AddTransient<{endpointClass.FullName}>();");
				if (endpointClass.Configure != null)
				{
					mapEndpoints.AddPartialStatement($"{endpointClass.FullName}.Configure(");
				}
				var minimalApiParams = new[] { "HttpContext context" }.Concat(request?.PropertyValuesFromMinimalApi ?? Enumerable.Empty<string>());
				mapEndpoints.AddStatement($"app.Map{endpoint.HttpMethod}({endpointClass.Path}, {(endpoint.NeedsAsync ? "async" : "")} ({string.Join(", ", minimalApiParams)}) =>");
				var mapContent = mapEndpoints.AddScope();

				if (endpointClass.CanBeSingleton)
				{
					endpointsInitRegion.AddStatement($"var {endpointClass.InstanceName} = app.Services.GetRequiredService<{endpointClass.FullName}>();");
				}
				else
				{
					mapContent.AddStatement($"var {endpointClass.InstanceName} = context.RequestServices.GetRequiredService<{endpointClass.FullName}>();");
				}
				foreach (var property in endpointClass.GetPropertiesNeedingInjected())
				{
					mapContent.AddStatement($"{endpointClass.InstanceName}.{property.Name} = {property.Type.GetInstanceOf()};");
				}
				if (request?.HasBody ?? false)
				{
					mapContent.AddStatement($"var body = await JsonSerializer.DeserializeAsync<{request.Name}Body>(context.Request.Body, jsonOptions);");
				}
				if (request != null)
				{
					var requestInit = mapContent.AddScope(new($"var request = new {request.FullName}", ";"));
					foreach (var property in request.Properties)
					{
						if (property.DataSource == ModelBindingSource.Body)
						{
							var defaultValue = property.DefaultValue ?? "default";
							requestInit.AddStatement($"{property.Property.Name} = body?.{property.Name} ?? {defaultValue}{(property.Property.NullableAnnotation == NullableAnnotation.NotAnnotated ? "!" : "")},");
						}
						else if (property.DataSource == ModelBindingSource.Route
							|| property.DataSource == ModelBindingSource.Query
							|| property.DataSource == ModelBindingSource.Header
							|| property.DataSource == ModelBindingSource.Form)
						{
							requestInit.AddStatement($"{property.Property.Name} = {property.SourceName},");
						}
						else
						{
							AddPropertyAssignment(property, requestInit);
						}
					}
				}
				var awaitCode = endpoint.IsTask ? "await " : "";

				var parameters = endpoint.GetInjectedParameters();
				if (request?.NeedsValidating ?? false)
				{
					mapContent.AddStatement($"var validationResult = await inst{request.ValidatorClass}.ValidateAsync(request);");
					if (!parameters.Contains("validationResult"))
					{
						var @if = mapContent.AddIf("!validationResult.IsValid");
						@if.AddStatement("return Results.ValidationProblem(validationResult.ToDictionary());");
					}
				}
				var typedReturn = endpoint.IsIResult ? "(IResult)" : "TypedResults.Ok";
				mapContent.AddStatement($"return {typedReturn}({awaitCode}{endpointClass.InstanceName}.{endpoint.HttpMethod}({string.Join(", ", parameters)}));");
				mapEndpoints.AddStatement(").WithMetadata(new Func<Voyager.OpenApi.VoyagerOpenApiMetadata>(() => ");
				AddOpenApiMetadata(mapEndpoints, endpoint, request);

				mapEndpoints.AddStatement($")()){(endpointClass.Configure == null ? "" : ")")};");
			}
		}

		servicesMethod.AddStatement($"services.AddTransient<IVoyagerMapping, Voyager.Generated.{context.Compilation.AssemblyName}Gen.EndpointMapper>();");

		return source.Build();
	}

	private static void GenerateRequestBodyClasses(ClassBuilder code, CodeBuilder endpointsInitRegion, RequestObject request)
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
			if (request.NeedsValidating)
			{
				CreateValidatorClass(code, request, endpointsInitRegion);
			}
		}
	}

	private static void CreateValidatorClass(ClassBuilder code, RequestObject request, CodeBuilder endpointsInitRegion)
	{
		var validatorClass = code.AddClass(new($"{request.ValidatorClass}", Access.Public));
		validatorClass.AddBase($"AbstractValidator<{request.FullName}>");
		var ctor = validatorClass.AddMethod(new($"{request.ValidatorClass}", "", Access.Public));
		var servicesArg = string.Empty;
		foreach (var prop in request.Properties)
		{
			if (prop.Property.NullableAnnotation == NullableAnnotation.NotAnnotated
				&& !prop.Property.Type.IsValueType)
			{
				ctor.AddStatement($"RuleFor(r => r.{prop.Name}).NotNull();");
			}
		}
		if (request.ValidationMethod != null)
		{
			if (request.ValidationMethod.Parameters.Length > 1)
			{
				ctor.AddParameter("IServiceProvider services");
				servicesArg = "app.Services";
			}
			List<string> parameters = [];
			foreach (var parameter in request.ValidationMethod.Parameters)
			{
				if (parameter.Type.ToDisplayString() == $"FluentValidation.AbstractValidator<{request.FullName}>")
				{
					parameters.Add("this");
				}
				else
				{
					ctor.AddStatement($"var {parameter.Name} = services.GetService<{parameter.Type.ToDisplayString()}>();");
					parameters.Add(parameter.Name);
				}
			}
			ctor.AddStatement($"{request.FullName}.{request.ValidationMethod.Name}({string.Join(", ", parameters)});");
		}
		endpointsInitRegion.AddStatement($"var inst{request.ValidatorClass} = new {request.ValidatorClass}({servicesArg});");
	}

	private void AddOpenApiMetadata(MethodBuilder mapEndpoints, Endpoint endpoint, RequestObject? request)
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
				metadata.AddStatement($"builder.AddBody(typeof({request.Name}Body));");
			}
		}

		metadata.AddStatement("builder.AddResponse(400, typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails));");
		foreach (var result in endpoint.FindResults())
		{
			metadata.AddStatement($"builder.AddResponse({result.StatusCode}, {(result.Type == null ? "null" : $"typeof({result.Type})")});");
		}
		metadata.AddStatement("return new Voyager.OpenApi.VoyagerOpenApiMetadata { Operation = builder.Build() };");
	}

	private void AddPropertyAssignment(RequestProperty property, CodeBuilder code)
	{
		var parts = property.Property.Type.ToDisplayParts();
		var typeName = property.Property.Type.ToDisplayString();
		var isEnumerable = property.Property.Type.Name == "IEnumerable";
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
		if (specializationType == "Object")
		{
			prefix = string.Empty;
			suffix = string.Empty;
			genericType = $"<{property.Property.Type.ToDisplayString().Trim('?')}>";
		}
		if (specializationType == "Number")
		{
			genericType = $"<{typeName}>";
		}
		var functionName = $"{prefix}Get{specializationType}{suffix}";
		var source = Enum.GetName(typeof(ModelBindingSource), property.DataSource);
		if (prefix == "Try")
		{
			code.AddStatement($"{property.Property.Name} = modelBinder.{functionName}{genericType}(context, ModelBindingSource.{source}, \"{property.SourceName}\"{(property.DefaultValue == null ? "" : $", {property.DefaultValue}")}, out var val{property.Name}) ? val{property.Name} : default,");
		}
		else
		{
			code.AddStatement($"{property.Property.Name} = modelBinder.{functionName}{genericType}(context, ModelBindingSource.{source}, \"{property.SourceName}\"{(property.DefaultValue == null ? "" : $", {property.DefaultValue}")}),");
		}
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

		public EndpointClass(ClassDeclarationSyntax syntax, SemanticModel semanticModel, AttributeSyntax attribute)
		{
			this.syntax = syntax;
			foreach (var (methodSyntax, httpMethod) in syntax.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>()
					.Select(m => (m, httpMethods.FirstOrDefault(httpMethod => m.Identifier.ToString().Equals(httpMethod, StringComparison.OrdinalIgnoreCase))))
					.Where((tuple) => tuple.Item2 != null))
			{
				endpointMethods.Add(new Endpoint(methodSyntax, semanticModel, httpMethod));
			}
			var configureSyntax = syntax.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>()
						.Where(m => m.Identifier.ToString() == "Configure").FirstOrDefault();
			if (configureSyntax != null)
			{
				Configure = new EndpointConfigureMethod();
			}
			classModel = semanticModel.GetDeclaredSymbol(syntax);
			FullName = classModel?.OriginalDefinition.ToString() ?? string.Empty;
			Path = attribute.ArgumentList?.Arguments[0].DescendantTokens().First().ToString() ?? string.Empty;
			FindProperties();
			CanBeSingleton = DetermineIfCanBeSingleton();
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

	internal class RequestObject
	{
		public IMethodSymbol? ValidationMethod { get; set; }
		public bool NeedsValidating => ValidationMethod != null || properties.Any(p => p.IsRequired);
		private readonly List<RequestProperty> properties = [];
		public IReadOnlyList<RequestProperty> Properties => properties;
		public IEnumerable<RequestProperty> BodyProperties => properties.Where(p => p.DataSource == ModelBindingSource.Body);
		public IEnumerable<string> PropertyValuesFromMinimalApi
		{
			get
			{
				return Properties.Select(GetMinimalApiParam).Where(p => p != null)!;
			}
		}

		private string? GetMinimalApiParam(RequestProperty prop)
		{
			var attr = (prop.DataSource != ModelBindingSource.Cookie && prop.DataSource != ModelBindingSource.Body)
				? prop.SourceAttribute : null;
			if (attr == null)
			{
				return null;
			}
			return $"{attr}{prop.Property.Type.ToDisplayString()} {prop.SourceName}";
		}

		public bool HasBody => properties.Any(p => p.DataSource == ModelBindingSource.Body);
		public string BodyClass => $"{Name}Body";
		public string ValidatorClass => $"{Name}Validator";
		public string Name { get; }
		public string FullName { get; }

		public RequestObject(string requestType, Microsoft.CodeAnalysis.TypeInfo requestTypeInfo)
		{
			var properties = requestTypeInfo.ConvertedType?.GetMembers().Where(m => m.Kind == SymbolKind.Property) ?? Enumerable.Empty<ISymbol>();
			foreach (var property in properties.OfType<IPropertySymbol>())
			{
				RequestProperty? requestProperty = null;
				foreach (var attribute in property.GetAttributes())
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
						requestProperty = new RequestProperty(property)
						{
							Attribute = attribute,
							DataSource = source,
							SourceAttribute = $"[{attribute}]"
						};
						break;
					}
				}
				requestProperty ??= new RequestProperty(property)
				{
					DataSource = ModelBindingSource.Body
				};
				if (property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is PropertyDeclarationSyntax syntax
						&& syntax.Initializer != null)
				{
					requestProperty.DefaultValue = syntax.Initializer.Value.ToString();
				}
				this.properties.Add(requestProperty);
			}

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
			}
			Name = requestType;
			FullName = requestTypeInfo.Type?.OriginalDefinition?.ToString() ?? Name;
		}
	}

	internal class RequestProperty(IPropertySymbol property)
	{
		public bool IsValueType => Property.Type.IsValueType;
		public string? DefaultValue { get; set; }
		public AttributeData? Attribute { get; set; }
		public ModelBindingSource DataSource { get; set; } = ModelBindingSource.Body;
		public string SourceAttribute { get; set; } = string.Empty;
		public bool IsRequired => Property.IsRequired;
		public IPropertySymbol Property { get; set; } = property;

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
		public bool NeedsAsync => IsTask || (Request != null && Request.NeedsValidating);
		private readonly string[] requestNames = ["request", "req"];
		public IEnumerable<string> GetInjectedParameters()
		{
			return method.ParameterList.Parameters
				.Select(p =>
				{
					if (requestNames.Any(rn => rn.Equals(p.Identifier.ValueText, StringComparison.Ordinal)))
					{
						return "request";
					}
					return semanticModel.GetTypeInfo(p.Type!).Type.GetInstanceOf();
				}).Where(p => p != null)!;
		}

		public Endpoint(MethodDeclarationSyntax method, SemanticModel semanticModel, string httpMethod)
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

			var requestTypeSyntax = method.ParameterList.Parameters.FirstOrDefault(p => requestNames.Any(rn => rn.Equals(p.Identifier.Text, StringComparison.OrdinalIgnoreCase)))?.Type;
			var requestType = string.Empty;
			if (requestTypeSyntax is IdentifierNameSyntax name)
			{
				requestType = name.Identifier.ToFullString().Trim();
				var requestTypeInfo = semanticModel.GetTypeInfo(requestTypeSyntax);
				Request = new RequestObject(requestType, requestTypeInfo);
			}
		}

		public bool IsIResult { get; set; } = false;
		public bool IsTask { get; set; } = false;
		public ITypeSymbol? ReturnType { get; set; }
		public string HttpMethod { get; }

		private IEnumerable<(string, string?)> GetResultFromType(ITypeSymbol? type)
		{
			if (type != null && type.ToDisplayString().StartsWith("Microsoft.AspNetCore.Http.HttpResults"))
			{
				if (type is INamedTypeSymbol namedSymbol && namedSymbol.TypeArguments.Any())
				{
					return new (string, string?)[] { ($"Microsoft.AspNetCore.Http.TypedResults.{type.Name}().StatusCode", namedSymbol.TypeArguments[0].ToDisplayString()) }; ;
				}
				return new (string, string?)[] { ($"Microsoft.AspNetCore.Http.TypedResults.{type.Name}().StatusCode", null) };
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
						var type = methodSymbol.IsGenericMethod ? methodSymbol.TypeArguments[0].ToDisplayString() : null;
						return new (string, string?)[] { ($"Microsoft.AspNetCore.Http.TypedResults.{methodSymbol.Name}().StatusCode", type) };
					}
				}
			}
			return Enumerable.Empty<(string, string?)>();
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
				results.Add(("200", ReturnType?.ToDisplayString()));
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