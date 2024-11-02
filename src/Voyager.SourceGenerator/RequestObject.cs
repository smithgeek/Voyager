using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Voyager.SourceGenerator;

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
