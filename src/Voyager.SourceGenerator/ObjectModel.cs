using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Voyager.SourceGenerator;

internal class ObjectModel
{
	private readonly ITypeSymbol typeSymbol;
	private readonly SemanticModel semanticModel;

	public bool IsRecord => typeSymbol.IsRecord;
	public List<PropertyModel> Properties { get; private set; } = [];

	public ObjectModel(ITypeSymbol typeSymbol, SemanticModel semanticModel)
	{
		this.typeSymbol = typeSymbol;
		this.semanticModel = semanticModel;
		GetProperties();
	}

	public string CreateDeclarationRecord(string name)
	{
		var record = new StringBuilder();
		record.Append($"public record {name} (");
		record.Append(string.Join(", ", Properties.Select(property => $"{property.ToDisplayType()} {property.Name}")));
		record.Append(");");
		return record.ToString();
	}

	private void GetProperties()
	{
		var recordParams = TypeHelper.GetRecordParameters(typeSymbol.DeclaringSyntaxReferences, semanticModel);
		var properties = typeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property) ?? [];
		foreach (var property in properties.OfType<IPropertySymbol>().Where(p => !p.IsImplicitlyDeclared))
		{
			recordParams.TryGetValue(property.Name, out var recordParam);
			Properties.Add(new PropertyModel(property, recordParam?.Attributes)
			{
				ConstructorIndex = recordParam?.Index,
				DefaultValue = recordParam?.Default
			});
		}
	}

}


internal class PropertyModel
{
	private readonly IPropertySymbol property;

	public PropertyModel(IPropertySymbol property, List<AttributeData>? additionalAttributes = null)
	{
		this.property = property;
		var attributes = property.GetAttributes().Concat(additionalAttributes ?? []);
		SearchForRequestAttributes(attributes);
		CheckForDefaultValue();
	}

	public bool Required => !property.Type.IsValueType && !property.Type.ToString().EndsWith("?");

	public string ToDisplayType()
	{
		return $"{Property.Type}";
	}

	private void CheckForDefaultValue()
	{
		if (DefaultValue == null)
		{
			foreach (var declaringSyntaxRef in property.DeclaringSyntaxReferences)
			{
				if (declaringSyntaxRef.GetSyntax() is PropertyDeclarationSyntax syntax
							&& syntax.Initializer != null)
				{
					DefaultValue = syntax.Initializer.Value.ToString();
					break;
				}
			}
		}
	}

	private void SearchForRequestAttributes(IEnumerable<AttributeData> attributes)
	{
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
				Attribute = attribute;
				DataSource = source;
				SourceAttribute = $"[{attribute}]";
				break;
			}
		}
	}

	public int? ConstructorIndex { get; set; } = null;
	public bool IsValueType => Property.Type.IsValueType;
	public string? DefaultValue { get; set; }
	public AttributeData? Attribute { get; set; }
	public ModelBindingSource DataSource { get; set; } = ModelBindingSource.Body;
	public string SourceAttribute { get; set; } = string.Empty;
	public bool IsRequired => Property.IsRequired;
	public IPropertySymbol Property => property;
	public bool IsNullable => property.NullableAnnotation == NullableAnnotation.Annotated;

	public string GetInitValue()
	{
		var defaultValue = DefaultValue == null ? string.Empty : $" ?? {DefaultValue}";
		if (DataSource == ModelBindingSource.Body)
		{
			var getFunc = GetBodyPropType();
			var modifier = IsNullable || !string.IsNullOrWhiteSpace(DefaultValue) ? "" :
				((getFunc == "String" || getFunc == null) ? "!" : "!.Value");
			getFunc ??= $"Deserialize<{ToDisplayType()}>";
			return $"body.MaybeGet{getFunc}(\"{SourceName}\"){modifier}{defaultValue}";
		}
		else if (DataSource == ModelBindingSource.Route
			|| DataSource == ModelBindingSource.Query
			|| DataSource == ModelBindingSource.Header
			|| DataSource == ModelBindingSource.Form)
		{
			return $"{SourceName}{defaultValue}";
		}
		else
		{
			return $"{GetValueFromModelBinder()}{defaultValue}";
		}
	}

	private static ITypeSymbol RemoveNullableWrapping(ITypeSymbol typeSymbol)
	{
		// Check if the type is a nullable type (e.g., int?)
		if (typeSymbol is INamedTypeSymbol namedType &&
			namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
		{
			// Return the underlying type (e.g., int for int?)
			return namedType.TypeArguments[0];
		}

		// If it's not nullable, return the original type
		return typeSymbol;
	}

	private string? GetBodyPropType()
	{
		return RemoveNullableWrapping(property.Type).SpecialType switch
		{
			SpecialType.System_String => "String",
			SpecialType.System_Boolean => "Bool",
			SpecialType.System_Byte => "Byte",
			SpecialType.System_SByte => "SByte",
			SpecialType.System_Decimal => "Decimal",
			SpecialType.System_Double => "Double",
			SpecialType.System_Int16 => "Int16",
			SpecialType.System_Int32 => "Int32",
			SpecialType.System_Int64 => "Int64",
			SpecialType.System_UInt16 => "UInt16",
			SpecialType.System_UInt32 => "UInt32",
			SpecialType.System_UInt64 => "UInt64",
			SpecialType.System_DateTime => "DateTime",
			SpecialType.System_Single => "Single",
			_ => null
		} ?? property.Type.Name switch
		{
			"System.Guid" => "Guid",
			"System.DateTimeOffset" => "DateTimeOffset",
			_ => null
		};
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

internal class RecordConstructorProperty
{
	public List<AttributeData> Attributes { get; set; } = [];
	public int? Index { get; set; }
	public string? Default { get; set; }
}

internal static class TypeHelper
{
	public static Dictionary<string, RecordConstructorProperty> GetRecordParameters(
		ImmutableArray<SyntaxReference> declarations, SemanticModel semanticModel)
	{
		var results = new Dictionary<string, RecordConstructorProperty>();
		foreach (var declarationRef in declarations)
		{
			GetRecordParameters(declarationRef.GetSyntax(), semanticModel, results);
		}
		return results;
	}

	public static Dictionary<string, RecordConstructorProperty> GetRecordParameters(
		SyntaxNode? declaration, SemanticModel semanticModel)
	{
		var results = new Dictionary<string, RecordConstructorProperty>();
		GetRecordParameters(declaration, semanticModel, results);
		return results;
	}

	private static void GetRecordParameters(
		SyntaxNode? declaration, SemanticModel semanticModel, Dictionary<string, RecordConstructorProperty> results)
	{
		if (declaration is RecordDeclarationSyntax recordDeclaration)
		{
			foreach (var parameter in recordDeclaration.ParameterList?.Parameters ?? [])
			{
				var defaultValue = parameter.Default?.Value.ToString();
				var name = parameter.Identifier.Text;
				var attributes = parameter.AttributeLists.SelectMany(attrList => attrList.Attributes)
					.Select(attr => GetAttributeData(attr, semanticModel)).Where(e => e != null).Select(e => e!).ToList();
				results[name] = new()
				{
					Attributes = attributes,
					Index = results.Count,
					Default = defaultValue
				};
			}
		}
	}

	private static AttributeData? GetAttributeData(AttributeSyntax attributeSyntax, SemanticModel semanticModel)
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
}
