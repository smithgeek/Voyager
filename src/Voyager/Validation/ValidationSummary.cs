using System.Collections.Generic;

namespace Voyager.Validation;

public class ValidationSummary(object? validationResult = null)
{
	public bool IsValid { get; init; }
	public Dictionary<string, string[]> Errors { get; init; } = [];
	public TValidationResult? GetValidationResult<TValidationResult>()
	{
		if (validationResult != null && validationResult is TValidationResult typedResult)
		{
			return typedResult;
		}
		return default;
	}
}