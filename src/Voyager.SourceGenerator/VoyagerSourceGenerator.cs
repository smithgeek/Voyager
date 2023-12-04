using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Voyager.SourceGenerator;

internal enum PropertyDataSource
{
	Query,
	Route,
	Form,
	Body
}

public static class Extensions
{
}

[Generator]
public class VoyagerSourceGenerator : ISourceGenerator
{
	public void Execute(GeneratorExecutionContext context)
	{
		var code = EndpointMapping(context);
		context.AddSource($"VoyagerEndpointMapping.g.cs", code);
	}

	public void Initialize(GeneratorInitializationContext context)
	{
	}

	private string EndpointMapping(GeneratorExecutionContext context)
	{
		var bodiesCreated = new HashSet<string>();
		var newClassesCode = new IndentedTextWriter(new StringWriter());
		newClassesCode.Indent++;
		newClassesCode.WriteLine();
		var addVoyagerCode = new IndentedTextWriter(new StringWriter());
		addVoyagerCode.WriteLine();
		addVoyagerCode.Indent++;

		addVoyagerCode.WriteLine("public static void AddVoyagerServices(IServiceCollection services)");
		addVoyagerCode.WriteLine("{");
		addVoyagerCode.WriteLine("\tAddVoyager(services);");
		addVoyagerCode.WriteLine("}");
		addVoyagerCode.WriteLine();
		addVoyagerCode.WriteLine("internal static void AddVoyager(this IServiceCollection services)");
		addVoyagerCode.WriteLine("{");
		addVoyagerCode.Indent++;
		var code = new IndentedTextWriter(new StringWriter());
		code.WriteLine("using FluentValidation;");
		code.WriteLine("using Microsoft.AspNetCore.Builder;");
		code.WriteLine("using Microsoft.AspNetCore.Http;");
		code.WriteLine("using Microsoft.AspNetCore.Routing;");
		code.WriteLine("using Microsoft.Extensions.DependencyInjection;");
		code.WriteLine("using Microsoft.OpenApi.Models;");
		code.WriteLine("using System.Collections.Generic;");
		code.WriteLine("using System.Threading;");
		code.WriteLine("using Voyager.ModelBinding;");
		code.WriteLine();
		code.WriteLine("namespace Voyager;");
		code.WriteLine();
		code.WriteLine("public static class VoyagerEndpoints");
		code.WriteLine("{");
		code.Indent++;
		code.WriteLine("public static void MapVoyagerEndpoints(WebApplication app)");
		code.WriteLine("{");
		code.WriteLine("\tMapVoyager(app);");
		code.WriteLine("}");
		code.WriteLine();
		code.WriteLine("internal static void MapVoyager(this WebApplication app)");
		code.WriteLine("{");
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

				var httpMethods = new[] { "Get", "Put", "Post", "Delete", "Patch" };
				foreach (var (method, httpMethod) in @class.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>()
					.Select(m => (m, httpMethods.FirstOrDefault(httpMethod => m.Identifier.ToString().Equals(httpMethod, StringComparison.OrdinalIgnoreCase))))
					.Where((tuple) => tuple.Item2 != null))
				{
					var configureMethod = @class.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>()
						.Where(m => m.Identifier.ToString() == "Configure").FirstOrDefault();

					var returnType = semanticModel.GetTypeInfo(method.ReturnType);
					var isTask = $"{returnType.Type?.ContainingNamespace}.{returnType.Type?.Name}" == "System.Threading.Tasks.Task";

					var requestTypeSyntax = method.ParameterList.Parameters.First().Type;
					var requestType = string.Empty;
					if (requestTypeSyntax is IdentifierNameSyntax name)
					{
						requestType = name.Identifier.ToFullString().Trim();
						var requestTypeInfo = semanticModel.GetTypeInfo(requestTypeSyntax);
						var requestObject = ParseRequestObject(requestTypeInfo);

						var classModel = semanticModel.GetDeclaredSymbol(@class);
						var path = attribute.ArgumentList?.Arguments[0].DescendantTokens().First().ToString();

						var hasBody = requestObject.Properties.Any(p => p.DataSource == PropertyDataSource.Body);

						addVoyagerCode.WriteLine($"services.AddTransient<{classModel?.OriginalDefinition}>();");
						code.Indent++;
						code.Write("");
						if (configureMethod != null)
						{
							code.Write($"{classModel?.OriginalDefinition}.Configure(");
						}
						code.WriteLine($"app.Map{httpMethod}({path}, async (HttpContext context) =>");
						code.WriteLine("{");
						code.Indent++;
						code.WriteLine($"var endpoint = context.RequestServices.GetRequiredService<{classModel?.OriginalDefinition}>();");
						InjectProperties(classModel, code);
						code.WriteLine("var modelBinder = context.RequestServices.GetService<IModelBinder>() ?? new ModelBinder(context);");
						var createRequestObject = !bodiesCreated.Contains($"{requestType}Body");
						if (createRequestObject)
						{
							bodiesCreated.Add($"{requestType}Body");
						}
						if (hasBody)
						{
							code.WriteLine($"var body = await modelBinder.GetBody<{requestType}Body>();");
							if (createRequestObject)
							{
								newClassesCode.WriteLine($"public class {requestType}Body");
								newClassesCode.WriteLine("{");
								newClassesCode.Indent++;
							}
						}
						code.WriteLine($"var request = new {requestTypeInfo.Type?.OriginalDefinition}");
						code.WriteLine("{");
						code.Indent++;
						foreach (var property in requestObject.Properties)
						{
							if (property.DataSource == PropertyDataSource.Body)
							{
								code.WriteLine($"{property.Property.Name} = body.{property.SourceName},");
								if (createRequestObject)
								{
									newClassesCode.WriteLine($"public {property.Property.Type} {property.SourceName} {{ get; set; }}");
								}
							}
							else
							{
								code.WriteLine($"{property.Property.Name} = {GetPropertyAssignment(property)},");
							}
						}
						if (hasBody && createRequestObject)
						{
							newClassesCode.Indent--;
							newClassesCode.WriteLine("}");
						}
						code.Indent--;
						code.WriteLine("};");
						var awaitCode = isTask ? "await " : "";

						var parameterTypes = method.ParameterList.Parameters.Skip(1).Select(p => GetInstanceOf(semanticModel.GetTypeInfo(p.Type).Type));
						var parameters = new[] { "request" }.Concat(parameterTypes.Where(p => p != null));
						if (requestObject.HasValidationMethod)
						{
							code.WriteLine($"var validator = new {requestType}Validator();");
							code.WriteLine($"{requestTypeInfo.Type?.OriginalDefinition}.AddValidationRules(validator);");
							code.WriteLine($"var validationResult = await validator.ValidateAsync(request);");
							if (!parameters.Contains("validationResult"))
							{
								code.WriteLine("if(!validationResult.IsValid)");
								code.WriteLine("{");
								code.WriteLine("\treturn Results.ValidationProblem(validationResult.ToDictionary());");
								code.WriteLine("}");
							}
						}
						code.WriteLine($"return {awaitCode}endpoint.{httpMethod}({string.Join(", ", parameters)});");
						code.Indent--;
						code.WriteLine("}).WithOpenApi(operation =>");
						code.WriteLine("{");
						code.Indent++;
						foreach (var property in requestObject.Properties.Where(p =>
							p.DataSource == PropertyDataSource.Query || p.DataSource == PropertyDataSource.Route))
						{
							var location = property.DataSource == PropertyDataSource.Query ? "Query" : "Path";
							code.WriteLine($"operation.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter");
							code.WriteLine($"{{");
							code.Indent++;
							code.WriteLine($"Name = \"{property.SourceName}\",");
							code.WriteLine($"In = Microsoft.OpenApi.Models.ParameterLocation.{location},");
							code.WriteLine($"Schema = Voyager.OpenApi.OpenApiSchemaGenerator.GenerateSchema(app.Services, typeof({property.Property.Type})),");
							code.Indent--;
							code.WriteLine($"}});");
						}
						if (hasBody)
						{
							code.WriteLine($"operation.RequestBody = new OpenApiRequestBody");
							WriteOpenApiContent(code, $"{requestType}Body");
							code.WriteLine("};");
						}
						code.WriteLine("operation.Responses = new OpenApiResponses();");
						code.WriteLine("operation.Responses.Add(\"200\", new OpenApiResponse");
						
						WriteOpenApiContent(code, returnType.Type?.ToDisplayString() ?? "");
						code.WriteLine("});");
						code.WriteLine("return operation;");
						code.Indent--;

						if (configureMethod != null)
						{
							code.WriteLine("}));");
						}
						else
						{
							code.WriteLine("});");
						}
						code.Indent--;

						if (requestObject.HasValidationMethod)
						{
							newClassesCode.WriteLine($"public class {requestType}Validator : AbstractValidator<{requestTypeInfo.Type?.OriginalDefinition}>");
							newClassesCode.WriteLine("{");
							newClassesCode.Indent++;
							newClassesCode.WriteLine($"public {requestType}Validator()");
							newClassesCode.WriteLine("{");
							newClassesCode.Indent++;
							foreach (var prop in requestObject.Properties)
							{
								if (prop.Property.IsRequired)
								{
									newClassesCode.WriteLine($"RuleFor(r => r.{prop.SourceName}).NotNull();");
								}
							}
							newClassesCode.Indent--;
							newClassesCode.WriteLine("}");

							newClassesCode.Indent--;
							newClassesCode.WriteLine("}");
						}
					}
				}
			}
		}

		code.WriteLine("}");
		code.WriteLine();
		code.WriteLineNoTabs(newClassesCode.InnerWriter.ToString());
		addVoyagerCode.Indent--;
		addVoyagerCode.WriteLine("}");
		code.WriteLineNoTabs(addVoyagerCode.InnerWriter.ToString());
		code.Indent--;
		code.WriteLine("}");
		return code.InnerWriter.ToString();
	}

	private static void WriteOpenApiContent(IndentedTextWriter code, string type)
	{
		code.WriteLine("{");
		code.Indent++;
		code.WriteLine("Content = new Dictionary<string, OpenApiMediaType>");
		code.WriteLine("{");
		code.Indent++;
		code.WriteLine("{");
		code.Indent++;
		code.WriteLine("\"application/json\",");
		code.WriteLine("new OpenApiMediaType");
		code.WriteLine("{");
		code.WriteLine($"\tSchema = Voyager.OpenApi.OpenApiSchemaGenerator.GenerateSchema(app.Services, typeof({type}))");
		code.WriteLine("}");
		code.Indent--;
		code.WriteLine("}");
		code.Indent--;
		code.WriteLine("}");
		code.Indent--;
	}

	private string? GetInstanceOf(ITypeSymbol? type)
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
			return "context.RequestServices.GetRequiredService<{typeName}>();";
		}
	}

	private string GetPropertyAssignment(RequestProperty property)
	{
		var parts = property.Property.Type.ToDisplayParts();
		var typeName = property.Property.Type.ToDisplayString();
		var isEnumerable = property.Property.Type.Name == "IEnumerable";
		var suffix = isEnumerable ? "s" : "";

		if (isEnumerable)
		{
			typeName = parts.Reverse().Skip(1).First().ToString();
		}
		var providerFunction = property.DataSource switch
		{
			PropertyDataSource.Query => $"GetQueryStringValue{suffix}",
			PropertyDataSource.Route => $"GetRouteValue{suffix}",
			PropertyDataSource.Form => $"GetRouteValue{suffix}",
			_ => "GetBodyValue",
		};
		return $"await modelBinder.{providerFunction}<{typeName}>(\"{property.SourceName}\")";
	}

	private void InjectProperties(INamedTypeSymbol? @class, TextWriter code)
	{
		if (@class != null)
		{
			var injectedProperties = @class?.GetMembers().Where(m =>
				m.Kind == SymbolKind.Property
				&& m is IPropertySymbol property
				&& (property.IsRequired ||
					property.GetAttributes().Any(attr => attr.AttributeClass?.ToString() == "Microsoft.AspNetCore.Mvc.FromServicesAttribute")))
				.OfType<IPropertySymbol>();
			if (injectedProperties != null)
			{
				foreach (var property in injectedProperties)
				{
					code.WriteLine($"endpoint.{property.Name} = {GetInstanceOf(property.Type)};");
				}
			}
		}
	}

	private RequestObject ParseRequestObject(Microsoft.CodeAnalysis.TypeInfo requestType)
	{
		var requestObject = new RequestObject();
		var properties = requestType.ConvertedType?.GetMembers().Where(m => m.Kind == SymbolKind.Property) ?? Enumerable.Empty<ISymbol>();
		foreach (var property in properties.OfType<IPropertySymbol>())
		{
			RequestProperty? requestProperty = null;
			foreach (var attribute in property.GetAttributes())
			{
				if (attribute.AttributeClass?.Name == "FromQueryAttribute")
				{
					requestProperty = new RequestProperty(property)
					{
						Attribute = attribute,
						DataSource = PropertyDataSource.Query
					};
					break;
				}
				if (attribute.AttributeClass?.Name == "FromRouteAttribute")
				{
					requestProperty = new RequestProperty(property)
					{
						Attribute = attribute,
						DataSource = PropertyDataSource.Route
					};
					break;
				}
				if (attribute.AttributeClass?.Name == "FromFormAttribute")
				{
					requestProperty = new RequestProperty(property)
					{
						Attribute = attribute,
						DataSource = PropertyDataSource.Form
					};
					break;
				}
			}
			requestProperty ??= new RequestProperty(property)
			{
				DataSource = PropertyDataSource.Body
			};
			requestObject.Properties.Add(requestProperty);
		}

		var validationMethods = requestType.ConvertedType?.GetMembers().Where(m => m.Kind == SymbolKind.Method
			&& m.Name.Equals("AddValidationRules", StringComparison.OrdinalIgnoreCase)).OfType<IMethodSymbol>();

		foreach (var validationMethod in validationMethods ?? Enumerable.Empty<IMethodSymbol>())
		{
			var firstParam = validationMethod.Parameters.FirstOrDefault();
			if (firstParam?.Type.ToDisplayString() == $"FluentValidation.AbstractValidator<{requestType.Type?.ToDisplayString()}>")
			{
				requestObject.HasValidationMethod = true;
			}
		}
		return requestObject;
	}

	internal class RequestObject
	{
		public bool HasValidationMethod { get; set; } = false;
		public List<RequestProperty> Properties { get; set; } = new();
	}

	internal class RequestProperty
	{
		public RequestProperty(IPropertySymbol property)
		{
			Property = property;
		}

		public AttributeData? Attribute { get; set; }
		public PropertyDataSource DataSource { get; set; } = PropertyDataSource.Body;
		public IPropertySymbol Property { get; set; }

		public string SourceName
		{
			get
			{
				if (Attribute != null && Attribute.ConstructorArguments.Length > 0)
				{
					return Attribute.ConstructorArguments[0].Value?.ToString() ?? Property.Name;
				}
				return Property.Name;
			}
		}
	}
}