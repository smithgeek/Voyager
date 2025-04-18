using Microsoft.CodeAnalysis;

namespace Voyager.SourceGenerator;

internal static class Extension
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
		else if (typeName == "Voyager.Validation.ValidationSummary")
		{
			return new InstanceInfo("validationResult") { IsValidationResult = true };
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

	public static string GetAssemblyName(this Compilation compilation)
	{
		return $"{compilation.AssemblyName?.Replace(".", "_") ?? string.Empty}_VoyagerSourceGen";
	}
}
