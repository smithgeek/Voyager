using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
		var requestBodies = new StringBuilder();
		var addVoyagerCode = new StringBuilder();
		addVoyagerCode.AppendLine();

		addVoyagerCode.AppendLine("\tpublic static void AddVoyagerServices(IServiceCollection services)");
		addVoyagerCode.AppendLine("\t{");
		addVoyagerCode.AppendLine("\t\tAddVoyager(services);");
		addVoyagerCode.AppendLine("\t}");
		addVoyagerCode.AppendLine("\tinternal static void AddVoyager(this IServiceCollection services)");
		addVoyagerCode.AppendLine("\t{");
		addVoyagerCode.AppendLine();
		var code = new StringBuilder();
		code.AppendLine("using Microsoft.AspNetCore.Builder;");
		code.AppendLine("using Microsoft.AspNetCore.Http;");
		code.AppendLine("using Microsoft.AspNetCore.Routing;");
		code.AppendLine("using Microsoft.Extensions.DependencyInjection;");
		code.AppendLine("using Microsoft.OpenApi.Models;");
		code.AppendLine("using System.Collections.Generic;");
		code.AppendLine("using System.Threading;");
		code.AppendLine("using Voyager.ModelBinding;");
		code.AppendLine();
		code.AppendLine("namespace Voyager;");
		code.AppendLine();
		code.AppendLine("public static class VoyagerEndpoints");
		code.AppendLine("{");
		code.AppendLine("\tpublic static void MapVoyagerEndpoints(WebApplication app)");
		code.AppendLine("\t{");
		code.AppendLine("\t\tMapVoyager(app);");
		code.AppendLine("\t}");
		code.AppendLine();
		code.AppendLine("\tinternal static void MapVoyager(this WebApplication app)");
		code.AppendLine("\t{");
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

						addVoyagerCode.AppendLine($"\t\tservices.AddTransient<{classModel?.OriginalDefinition}>();");
						code.Append("\t\t");
						if (configureMethod != null)
						{
							code.Append($"{classModel?.OriginalDefinition}.Configure(");
						}
						code.AppendLine($"app.Map{httpMethod}({path}, async (HttpContext context, CancellationToken cancellationToken) =>");
						code.AppendLine("\t\t{");
						code.AppendLine($"\t\t\tvar endpoint = context.RequestServices.GetRequiredService<{classModel?.OriginalDefinition}>();");
						code.Append(InjectProperties(classModel));
						code.AppendLine("\t\t\tvar modelBinder = context.RequestServices.GetService<IModelBinder>() ?? new ModelBinder(context);");
						var createRequestObject = !bodiesCreated.Contains($"{requestType}Body");
						if (createRequestObject)
						{
							bodiesCreated.Add($"{requestType}Body");
						}
						if (hasBody)
						{
							code.AppendLine($"\t\t\tvar body = await modelBinder.GetBody<{requestType}Body>();");
							if (createRequestObject)
							{
								requestBodies.AppendLine($"\tpublic class {requestType}Body");
								requestBodies.AppendLine("\t{");
							}
						}
						code.AppendLine($"\t\t\tvar request = new {requestTypeInfo.Type?.OriginalDefinition}");
						code.AppendLine("\t\t\t{");
						foreach (var property in requestObject.Properties)
						{
							if (property.DataSource == PropertyDataSource.Body)
							{
								code.AppendLine($"\t\t\t\t{property.Property.Name} = body.{property.SourceName},");
								if (createRequestObject)
								{
									requestBodies.AppendLine($"\t\tpublic {property.Property.Type} {property.SourceName} {{ get; set; }}");
								}
							}
							else
							{
								code.AppendLine($"\t\t\t\t{property.Property.Name} = {GetPropertyAssignment(property)},");
							}
						}
						if (hasBody && createRequestObject)
						{
							requestBodies.AppendLine("\t}");
						}
						code.AppendLine("\t\t\t};");
						var awaitCode = isTask ? "await " : "";

						var parameterTypes = method.ParameterList.Parameters.Skip(1).Select(p => GetInstanceOf(semanticModel.GetTypeInfo(p.Type).Type));
						var parameters = new[] { "request" }.Concat(parameterTypes.Where(p => p != null));
						code.AppendLine($"\t\t\treturn {awaitCode}endpoint.{httpMethod}({string.Join(", ", parameters)});");
						code.AppendLine("\t\t}).WithOpenApi(operation =>");
						code.AppendLine("\t\t{");
						foreach (var property in requestObject.Properties.Where(p =>
							p.DataSource == PropertyDataSource.Query || p.DataSource == PropertyDataSource.Route))
						{
							var location = property.DataSource == PropertyDataSource.Query ? "Query" : "Path";
							code.AppendLine($"\t\t\toperation.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter");
							code.AppendLine($"\t\t\t{{");
							code.AppendLine($"\t\t\t\tName = \"{property.SourceName}\",");
							code.AppendLine($"\t\t\t\tIn = Microsoft.OpenApi.Models.ParameterLocation.{location},");
							code.AppendLine($"\t\t\t\tSchema = Voyager.OpenApi.OpenApiSchemaGenerator.GenerateSchema(app.Services, typeof({property.Property.Type})),");
							code.AppendLine($"\t\t\t}});");
						}
						if (hasBody)
						{
							code.AppendLine($"\t\t\toperation.RequestBody = new OpenApiRequestBody");
							code.AppendLine("\t\t\t{");
							code.AppendLine("\t\t\t\tContent = new Dictionary<string, OpenApiMediaType>");
							code.AppendLine("\t\t\t\t{");
							code.AppendLine("\t\t\t\t\t{");
							code.AppendLine("\t\t\t\t\t\t\"application/json\",");
							code.AppendLine("\t\t\t\t\t\tnew OpenApiMediaType");
							code.AppendLine("\t\t\t\t\t\t{");
							code.AppendLine($"\t\t\t\t\t\t\tSchema = Voyager.OpenApi.OpenApiSchemaGenerator.GenerateSchema(app.Services, typeof({requestType}Body))");
							code.AppendLine("\t\t\t\t\t\t}");
							code.AppendLine("\t\t\t\t\t}");
							code.AppendLine("\t\t\t\t}");
							code.AppendLine("\t\t\t};");
						}
						code.AppendLine("\t\t\treturn operation;");

						if (configureMethod != null)
						{
							code.AppendLine("\t\t}));");
						}
						else
						{
							code.AppendLine("\t\t});");
						}
					}
				}
			}
		}

		code.AppendLine("\t}");
		code.AppendLine();
		code.Append(requestBodies);
		addVoyagerCode.AppendLine("\t}");
		code.Append(addVoyagerCode);
		code.AppendLine("}");
		return code.ToString();
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
			return "cancellationToken";
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

	private string InjectProperties(INamedTypeSymbol? @class)
	{
		var code = new StringBuilder();
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
					code.AppendLine($"\t\t\tendpoint.{property.Name} = {GetInstanceOf(property.Type)};");
				}
			}
		}
		return code.ToString();
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
		return requestObject;
	}

	internal class RequestObject
	{
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