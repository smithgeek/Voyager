using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Voyager.SourceGenerator;

internal class EndpointClass
{
	private readonly ClassDeclarationSyntax syntax;
	private readonly string[] httpMethods = ["Get", "Put", "Post", "Delete", "Patch"];
	private readonly List<Endpoint> endpointMethods = [];
	public IReadOnlyList<Endpoint> EndpointMethods => endpointMethods;
	public bool HasConfigureMethod { get; } = false;
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
			HasConfigureMethod = true;
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
