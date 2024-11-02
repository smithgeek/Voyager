using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Voyager.SourceGenerator;

internal class Endpoint
{
	private const string IResultInterface = "Microsoft.AspNetCore.Http.IResult";

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
