using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voyager.SourceGenerator;

internal enum PropertyDataSource
{
	Query,
	Route,
	Form,
	Body
}

internal class PropertyDeclaration
{
	public PropertyDataSource DataSource { get; set; }
	public PropertyDeclarationSyntax Property { get; set; }
	public string Name { get; private set; }
	public string TypeName = "";
	public string? DefaultValue => Property.Initializer?.Value.ToString();

	public PropertyDeclaration(PropertyDeclarationSyntax property, AttributeSyntax? attribute)
	{
		Property = property;
		Name = GetName(attribute) ?? property.Identifier.Text;
	}

	private string? GetName(AttributeSyntax? attribute)
	{
		if (attribute?.ArgumentList?.Arguments.Count == 1
			&& attribute.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax expression)
		{
			return expression.Token.Text.Trim(new[] { '"' });
		}
		return null;
	}
}



[Generator]
public class VoyagerSourceGenerator : ISourceGenerator
{
	private static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceSymbol @namespace)
	{
		return @namespace.GetTypeMembers()
			.Concat(@namespace.GetNamespaceMembers().SelectMany(GetTypes));
	}

	private static IEnumerable<ITypeSymbol> GetFactoryTypes(GeneratorExecutionContext context)
	{
		var assemblies = context.Compilation.SourceModule.ReferencedAssemblySymbols;
		var types = assemblies.SelectMany(a => GetTypes(a.GlobalNamespace)).ToList();
		return types.Where(t => t.OriginalDefinition.ToString().StartsWith("Voyager.AssemblyFactories.")
			&& t.OriginalDefinition.ToString() != "Voyager.AssemblyFactories.Registration");
	}

	private IEnumerable<ISymbol> GetAllProperties(ITypeSymbol type)
	{
		var properties = type.GetMembers().Where(s => s.Kind == SymbolKind.Property);
		if (type.BaseType != null)
		{
			return properties.Concat(GetAllProperties(type.BaseType));
		}
		return properties;
	}

	private IEnumerable<ClassDeclarationSyntax> GetClassesImplementing(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classDeclarations, string interfaceName)
	{
		return classDeclarations.Where(cd =>
		{
			var root = cd.SyntaxTree.GetRoot();
			var model = compilation.GetSemanticModel(root.SyntaxTree);
			var declaredSymbol = model.GetDeclaredSymbol(root.DescendantNodes().OfType<ClassDeclarationSyntax>()
				.FirstOrDefault(d => d.Identifier == cd.Identifier));

			var interfaces = (declaredSymbol as ITypeSymbol)?.AllInterfaces;
			return interfaces?.Any(i => $"{i.ContainingNamespace}.{i.Name}" == interfaceName) ?? false;
		});
	}

	private string ConfigureAuthorizationHandlers(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classDeclarations)
	{
		var symbols = GetClassesImplementing(compilation, classDeclarations, "Microsoft.AspNetCore.Authorization.IAuthorizationHandler");

		var builder = new StringBuilder();
		foreach (var symbol in symbols)
		{
			builder.AppendLine($"\t\t\tservices.AddTransient<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, {GetNamespace(symbol)}.{symbol.Identifier}>();");
		}
		return builder.ToString();
	}

	private string GetPolicies(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classDeclarations)
	{
		var symbols = GetClassesImplementing(compilation, classDeclarations, "Voyager.Api.Authorization.Policy");

		var builder = new StringBuilder();
		builder.AppendLine("\t\tpublic static void RegisterPolicies(Dictionary<string, Voyager.Api.Authorization.Policy> policies)");
		builder.AppendLine("\t\t{");
		foreach (var policySymbol in symbols)
		{
			var fullName = $"{GetNamespace(policySymbol)}.{policySymbol.Identifier}";
			builder.AppendLine($"\t\t\tpolicies[\"{fullName}\"] = new {fullName}();");
		}
		builder.AppendLine("\t\t}");
		builder.AppendLine();
		return builder.ToString();
	}

	//private string GetAuthorizationHandlers(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classDeclarations)
	//{
	//	var classes = GetClassesImplementing(compilation, classDeclarations, "Microsoft.AspNetCore.Authorization.IAuthorizationHandler");
	//	var builder = new StringBuilder();
	//	builder.AppendLine("services.AddScoped(typeof(IAuthorizationHandler), type);");
	//}

	private string CreateConfigureServices(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classDeclarations, string parentClassName)
	{
		var symbols = classDeclarations.Select(cd =>
		{
			var root = cd.SyntaxTree.GetRoot();
			var model = compilation.GetSemanticModel(root.SyntaxTree);
			var declaredSymbol = model.GetDeclaredSymbol(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First());

			var symbol = compilation.GetSemanticModel(cd.SyntaxTree).GetDeclaredSymbol(cd);
			List<(string Type, string Name)> propertyDefinitions = new();
			if (symbol is ITypeSymbol type)
			{
				var properties = GetAllProperties(type);
				foreach (var propertySymbol in properties)
				{
					if (propertySymbol is IPropertySymbol property
						&& property.GetAttributes().Any(a => a.AttributeClass?.Name == "VoyagerInjectAttribute"))
					{
						var ptype = $"{property.Type.ContainingNamespace}.{property.Type.Name}";
						var name = property.Name;
						propertyDefinitions.Add((ptype, name));
					}
				}
			}


			var interfaces = (declaredSymbol as ITypeSymbol)?.AllInterfaces;
			return (Symbol: cd, IsEndpoint: interfaces?.Any(i => $"{i.ContainingNamespace}.{i.Name}" == "Voyager.Api.IEndpointHandler") ?? false,
				RequestInterface: interfaces?.FirstOrDefault(i => $"{i.ContainingNamespace}.{i.Name}" == "MediatR.IRequestHandler"),
				Properties: propertyDefinitions);
		}).Where((tuple) =>
		{
			return tuple.IsEndpoint;
		});
		var builder = new StringBuilder();
		builder.AppendLine("\t\tpublic static void ConfigureServices(IServiceCollection services, AddVoyagerOptions options)");
		builder.AppendLine("\t\t{");
		foreach (var (Symbol, _, RequestInterface, Properties) in symbols)
		{
			builder.AppendLine($"\t\t\tservices.AddTransient<{GetNamespace(Symbol)}.{Symbol.Identifier}>();");
			builder.AppendLine($"\t\t\tservices.AddTransient<{RequestInterface}>(sp => ");
			builder.AppendLine("\t\t\t{");
			builder.AppendLine($"\t\t\t\tvar instance = sp.GetRequiredService<{GetNamespace(Symbol)}.{Symbol.Identifier}>();");
			foreach (var property in Properties)
			{
				if (property.Type == "Microsoft.AspNetCore.Http.HttpContext")
				{
					builder.AppendLine($"\t\t\t\tinstance.{property.Name} = sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext;");
				}
				else
				{
					builder.AppendLine($"\t\t\t\tinstance.{property.Name} = sp.GetRequiredService<{property.Type}>();");
				}
			}
			builder.AppendLine("\t\t\t\treturn instance;");
			builder.AppendLine("\t\t\t});");
		}
		builder.Append(ConfigureAuthorizationHandlers(compilation, classDeclarations));
		builder.AppendLine($"\t\t\tservices.AddValidatorsFromAssemblies(new []{{typeof({parentClassName}).Assembly}});");
		builder.AppendLine("\t\t\tif(options.RegisterAllMediator)");
		builder.AppendLine("\t\t\t{");
		builder.AppendLine($"\t\t\t\tservices.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof({parentClassName}).Assembly));");
		builder.AppendLine("\t\t\t}");
		builder.AppendLine("\t\t}");
		builder.AppendLine();
		return builder.ToString();
	}

	public void Execute(GeneratorExecutionContext context)
	{
		var referencedFactories = GetFactoryTypes(context);

		var allNodes = context.Compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
		var allClasses = allNodes
			.Where(d => d.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration))
			.OfType<ClassDeclarationSyntax>();

		var routeClasses = allClasses
			.Where(c => c.AttributeLists.SelectMany(x => x.Attributes).Any(attr => attr.Name.ToFullString() == "VoyagerRoute"));



		var assemblyFactorySource = new StringBuilder();
		assemblyFactorySource.AppendLine("using FluentValidation;");
		assemblyFactorySource.AppendLine("using Microsoft.Extensions.DependencyInjection;");
		assemblyFactorySource.AppendLine("using System;");
		assemblyFactorySource.AppendLine("using System.Collections.Generic;");
		assemblyFactorySource.AppendLine("using Voyager.Api;");
		assemblyFactorySource.AppendLine();
		assemblyFactorySource.AppendLine("namespace Voyager.AssemblyFactories");
		assemblyFactorySource.AppendLine("{");
		var assemblyName = context.Compilation.AssemblyName?.Replace(".", "_") ?? string.Empty;
		var assemblyFactoryClassName = $"{assemblyName}VoyagerFactory";
		assemblyFactorySource.AppendLine("\t[Voyager.Factories.RequestFactory]");
		assemblyFactorySource.AppendLine($"\tpublic static class {assemblyFactoryClassName}");
		assemblyFactorySource.AppendLine("\t{");
		assemblyFactorySource.Append(CreateConfigureServices(context.Compilation, allClasses, assemblyFactoryClassName));
		assemblyFactorySource.Append(GetPolicies(context.Compilation, allClasses));
		assemblyFactorySource.AppendLine("\t\tpublic static void Register(List<VoyagerRouteRegistration> registrations)");
		assemblyFactorySource.AppendLine("\t\t{");

		foreach (var route in routeClasses)
		{
			var endpointRequest = route.BaseList?.Types.FirstOrDefault(t =>
			{
				if (t.Type is GenericNameSyntax genericName)
				{
					return genericName.Identifier.ToString() == "EndpointRequest";
				}
				return false;
			});
			string? returnType = null;
			if (endpointRequest != null && endpointRequest.Type is GenericNameSyntax genericName)
			{
				returnType = genericName.TypeArgumentList.Arguments.First().ToString();
			}

			var validationMethod = route.Members.Where(m => m.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration)
				.Cast<MethodDeclarationSyntax>()
				.FirstOrDefault(m =>
				{
					return m.Identifier.Text == "AddValidationRules" && m.Modifiers.Any(modifier => modifier.Text == "static");
				});

			var properties = route.Members.Where(m => m.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration)
				.Cast<PropertyDeclarationSyntax>()
				.Select(p =>
				{
					var model = context.Compilation.GetSemanticModel(p.SyntaxTree);
					var psymbol = model.GetDeclaredSymbol(p) as IPropertySymbol;
					var typeName = psymbol?.Type.ToDisplayString() ?? string.Empty;

					var attributes = p.AttributeLists.SelectMany(x => x.Attributes);
					var routeAttribute = attributes.FirstOrDefault(a => a.Name.ToString() == "FromRoute");
					if (routeAttribute != null)
					{
						return new PropertyDeclaration(p, routeAttribute)
						{
							DataSource = PropertyDataSource.Route,
							TypeName = typeName
						};
					}

					var queryAttribute = attributes.FirstOrDefault(a => a.Name.ToString() == "FromQuery");
					if (queryAttribute != null)
					{
						return new PropertyDeclaration(p, queryAttribute)
						{
							DataSource = PropertyDataSource.Query,
							TypeName = typeName
						};
					}

					var formAttribute = attributes.FirstOrDefault(a => a.Name.ToString() == "FromForm");
					if (formAttribute != null)
					{
						return new PropertyDeclaration(p, formAttribute)
						{
							DataSource = PropertyDataSource.Form,
							TypeName = typeName
						};
					}

					var bodyAttribute = attributes.FirstOrDefault(a => a.Name.ToString() == "FromBody");
					return new PropertyDeclaration(p, bodyAttribute)
					{
						DataSource = PropertyDataSource.Body,
						TypeName = typeName
					};
				});

			var bodyProperties = properties.Where(p => p.DataSource == PropertyDataSource.Body);
			var hasBody = bodyProperties.Any();

			var routeAttribute = route.AttributeLists.SelectMany(x => x.Attributes).FirstOrDefault(attr => attr.Name.ToFullString() == "VoyagerRoute");
			var routeMethod = routeAttribute.ArgumentList?.Arguments[0].ToString()?.Split('.').LastOrDefault() ?? string.Empty;
			var routePath = routeAttribute.ArgumentList?.Arguments[1].ToString() ?? string.Empty;

			var routeSource = new StringBuilder();
			routeSource.AppendLine("// <auto-generated />");
			routeSource.AppendLine("using System;");
			routeSource.AppendLine("using System.Collections.Generic;");
			routeSource.AppendLine("using System.Threading.Tasks;");
			routeSource.AppendLine("using Voyager;");
			routeSource.AppendLine("using Voyager.Api;");
			var routeNamespace = GetNamespace(route);
			routeSource.AppendLine($"using {routeNamespace};");
			routeSource.AppendLine($"namespace {routeNamespace}.VoyagerFactories;");
			routeSource.AppendLine();
			var className = $"{route.Identifier.Text}Factory";
			routeSource.AppendLine($"public static class {className}");
			routeSource.AppendLine("{");
			routeSource.AppendLine($"\tpublic static async Task<object> CreateRequestObject(DataProvider dataProvider)");
			routeSource.AppendLine("\t{");
			if (hasBody)
			{
				routeSource.AppendLine($"\t\tvar body = await dataProvider.GetBody<RequestBody>();");
			}
			routeSource.AppendLine($"\t\treturn new {route.Identifier.Text}");
			routeSource.AppendLine("\t\t{");

			var descriptorSource = new StringBuilder();
			descriptorSource.AppendLine("\tpublic static VoyagerApiDescription CreateApiDescription()");
			descriptorSource.AppendLine("\t{");
			descriptorSource.AppendLine($"\t\treturn new VoyagerApiDescription");
			descriptorSource.AppendLine("\t\t{");
			descriptorSource.AppendLine($"\t\t\tAssemblyName = \"{context.Compilation.AssemblyName}\",");
			if (hasBody)
			{
				descriptorSource.AppendLine("\t\t\tBodyType = typeof(RequestBody),");
			}
			descriptorSource.AppendLine($"\t\t\tMethod = \"{routeMethod}\",");
			descriptorSource.AppendLine($"\t\t\tPath = {routePath},");
			descriptorSource.AppendLine($"\t\t\tProperties = new List<VoyagerApiDescription.Property>");
			descriptorSource.AppendLine("\t\t\t{");


			var bodyTypeSource = new StringBuilder();
			if (hasBody)
			{
				bodyTypeSource.AppendLine("\tprivate class RequestBody");
				bodyTypeSource.AppendLine("\t{");
				foreach (var property in bodyProperties)
				{
					bodyTypeSource.AppendLine($"\t\tpublic {property.TypeName} {property.Name} {{ get; init; }}");
				}
				bodyTypeSource.AppendLine("\t}");
				bodyTypeSource.AppendLine();
			}

			foreach (var property in properties)
			{
				var propertyName = property.Property.Identifier.Text;
				if (property.DataSource == PropertyDataSource.Body)
				{
					routeSource.AppendLine($"\t\t\t{propertyName} = body.{property.Name},");
				}
				else
				{
					routeSource.AppendLine($"\t\t\t{propertyName} = {GetPropertyAssignment(property)},");
				}

				descriptorSource.AppendLine("\t\t\t\tnew VoyagerApiDescription.Property");
				descriptorSource.AppendLine("\t\t\t\t{");
				descriptorSource.AppendLine($"\t\t\t\t\tName = \"{property.Name}\",");
				descriptorSource.AppendLine($"\t\t\t\t\tType = typeof({property.TypeName}),");
				descriptorSource.AppendLine($"\t\t\t\t\tSource = \"{GetBindingSource(property)}\",");
				descriptorSource.AppendLine($"\t\t\t\t\tDescription = \"{propertyName}\",");
				descriptorSource.AppendLine($"\t\t\t\t\tDefaultValue = \"{property.DefaultValue}\",");
				descriptorSource.AppendLine($"\t\t\t\t\tParentType = typeof({route.Identifier.Text}),");
				descriptorSource.AppendLine($"\t\t\t\t\tPropertyName = \"{propertyName}\",");
				descriptorSource.AppendLine("\t\t\t\t},");
			}
			routeSource.AppendLine("\t\t};");
			routeSource.AppendLine("\t}");
			routeSource.AppendLine();

			descriptorSource.AppendLine("\t\t\t},");
			descriptorSource.AppendLine($"\t\t\tRequestTypeName = \"{className}\",");
			if (returnType != null)
			{
				descriptorSource.AppendLine($"\t\t\tResponseType = typeof({returnType}),");
			}
			descriptorSource.AppendLine("\t\t};");
			descriptorSource.AppendLine("\t}");
			descriptorSource.AppendLine("");

			routeSource.Append(bodyTypeSource.ToString());
			routeSource.Append(descriptorSource.ToString());
			routeSource.AppendLine("}");

			if (validationMethod != null)
			{
				routeSource.AppendLine();
				routeSource.AppendLine($"public class {route.Identifier.Text}Validator : FluentValidation.AbstractValidator<{route.Identifier.Text}>");
				routeSource.AppendLine("{");
				routeSource.AppendLine($"\tpublic {route.Identifier.Text}Validator()");
				routeSource.AppendLine("\t{");
				routeSource.AppendLine($"\t\t{route.Identifier.Text}.AddValidationRules(this);");
				routeSource.AppendLine("\t}");
				routeSource.AppendLine("}");
			}

			assemblyFactorySource.AppendLine($"\t\t\tregistrations.Add(new VoyagerRouteRegistration");
			assemblyFactorySource.AppendLine("\t\t\t{");
			assemblyFactorySource.AppendLine($"\t\t\t\tRequestFactory = {routeNamespace}.VoyagerFactories.{className}.CreateRequestObject,");
			assemblyFactorySource.AppendLine($"\t\t\t\tDescriptionFactory = {routeNamespace}.VoyagerFactories.{className}.CreateApiDescription,");
			assemblyFactorySource.AppendLine($"\t\t\t\tRouteDefinition = new VoyagerRouteDefinition");
			assemblyFactorySource.AppendLine("\t\t\t\t{");
			assemblyFactorySource.AppendLine($"\t\t\t\t\tMethod = \"{routeMethod}\",");
			assemblyFactorySource.AppendLine($"\t\t\t\t\tRequestType = typeof({routeNamespace}.{route.Identifier.Text}),");
			assemblyFactorySource.AppendLine($"\t\t\t\t\tTemplate = {routePath},");
			assemblyFactorySource.AppendLine("\t\t\t\t}");
			assemblyFactorySource.AppendLine("\t\t\t});");

			var code = routeSource.ToString();
			context.AddSource($"{route.Identifier.Text}.g.cs", routeSource.ToString());
		}
		assemblyFactorySource.AppendLine("\t\t}");
		assemblyFactorySource.AppendLine("\t}");
		assemblyFactorySource.AppendLine();
		assemblyFactorySource.AppendLine("\tinternal static class Registration");
		assemblyFactorySource.AppendLine("\t{");
		assemblyFactorySource.AppendLine("\t\tpublic static List<VoyagerRouteRegistration> GetAllRoutes()");
		assemblyFactorySource.AppendLine("\t\t{");
		assemblyFactorySource.AppendLine("\t\t\tvar registrations = new List<VoyagerRouteRegistration>();");
		assemblyFactorySource.AppendLine($"\t\t\t{assemblyFactoryClassName}.Register(registrations);");
		foreach (var factory in referencedFactories)
		{
			assemblyFactorySource.AppendLine($"\t\t\t{factory.OriginalDefinition}.Register(registrations);");
		}
		assemblyFactorySource.AppendLine("\t\t\treturn registrations;");
		assemblyFactorySource.AppendLine("\t\t}");
		assemblyFactorySource.AppendLine();

		assemblyFactorySource.AppendLine("\t\tpublic static Dictionary<string, Voyager.Api.Authorization.Policy> GetPolicies()");
		assemblyFactorySource.AppendLine("\t\t{");
		assemblyFactorySource.AppendLine("\t\t\tvar policies = new Dictionary<string, Voyager.Api.Authorization.Policy>();");
		assemblyFactorySource.AppendLine($"\t\t\t{assemblyFactoryClassName}.RegisterPolicies(policies);");
		foreach (var factory in referencedFactories)
		{
			assemblyFactorySource.AppendLine($"\t\t\t{factory.OriginalDefinition}.RegisterPolicies(policies);");
		}
		assemblyFactorySource.AppendLine("\t\t\treturn policies;");
		assemblyFactorySource.AppendLine("\t\t}");
		assemblyFactorySource.AppendLine();

		assemblyFactorySource.AppendLine("\t\tpublic static IServiceCollection AddVoyager(this IServiceCollection services, AddVoyagerOptions options = null)");
		assemblyFactorySource.AppendLine("\t\t{");
		assemblyFactorySource.AppendLine($"\t\t\toptions ??= new AddVoyagerOptions();");
		assemblyFactorySource.AppendLine($"\t\t\tservices.AddTransient(sp => GetAllRoutes());");
		assemblyFactorySource.AppendLine($"\t\t\t{assemblyFactoryClassName}.ConfigureServices(services, options);");
		foreach (var factory in referencedFactories)
		{
			assemblyFactorySource.AppendLine($"\t\t\t{factory.OriginalDefinition}.ConfigureServices(services, options);");
		}
		assemblyFactorySource.AppendLine("\t\t\tvar policies = GetPolicies();");
		assemblyFactorySource.AppendLine("\t\t\tif(options?.RegisterPolicies != null)");
		assemblyFactorySource.AppendLine("\t\t\t{");
		assemblyFactorySource.AppendLine("\t\t\t\toptions.RegisterPolicies(policies);");
		assemblyFactorySource.AppendLine("\t\t\t}");
		assemblyFactorySource.AppendLine("\t\t\tVoyager.AssemblyFactories.VoyagerFactory.AddAuthorization(services, policies);");
		assemblyFactorySource.AppendLine("\t\t\treturn services;");
		assemblyFactorySource.AppendLine("\t\t}");
		assemblyFactorySource.AppendLine("\t}");
		assemblyFactorySource.AppendLine("}");

		context.AddSource($"{assemblyFactoryClassName}.g.cs", assemblyFactorySource.ToString());
	}

	private string GetNamespace(ClassDeclarationSyntax classDeclaration)
	{
		var node = classDeclaration.Parent;
		while (node != null && node is not NamespaceDeclarationSyntax)
		{
			node = node?.Parent;
		}
		if (node != null && node is NamespaceDeclarationSyntax namespaceDeclaration)
		{
			return namespaceDeclaration.Name.ToString();
		}
		return string.Empty;
	}

	private string GetPropertyAssignment(PropertyDeclaration property)
	{
		var providerFunction = property.DataSource switch
		{
			PropertyDataSource.Query => "GetQueryStringValue",
			PropertyDataSource.Route => "GetRouteValue",
			PropertyDataSource.Form => "GetRouteValue",
			_ => "GetBodyValue",
		};
		return $"await dataProvider.{providerFunction}<{property.TypeName}>(\"{property.Name}\")";
	}

	private string GetBindingSource(PropertyDeclaration property)
	{
		return property.DataSource switch
		{
			PropertyDataSource.Query => "Query",
			PropertyDataSource.Body => "Body",
			PropertyDataSource.Form => "Form",
			PropertyDataSource.Route => "Path",
			_ => string.Empty
		};
	}

	public void Initialize(GeneratorInitializationContext context)
	{
	}
}