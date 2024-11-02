using Microsoft.CodeAnalysis;
using System.Linq;

namespace Voyager.SourceGenerator;

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
					};
				}
				return new($"TypedResults.{typeSymbol.Name}().StatusCode.ToString()", new(typeSymbol, semanticModel));
			}
			else if (typeSymbol != null)
			{
				return new(statusCode ?? "200", new(typeSymbol, semanticModel))
				{
					FullTypeName = typeSymbol.ToDisplayString(),
					IsGeneratedType = typeSymbol.IsAnonymousType
				};
			}
		}
		return null;
	}
}
