namespace Voyager.Validation.Validot;

using global::Validot;

internal class ValidotValidationAdapter<TRequest>(IValidator<TRequest> validator) : IVoyagerValidationAdapter<TRequest>
{
	public Task<ValidationSummary> Validate(TRequest request, Func<string, string> getPropertyName)
	{
		var result = validator.Validate(request);
		return Task.FromResult(new ValidationSummary(result)
		{
			IsValid = !result.AnyErrors,
			Errors = result.MessageMap.ToDictionary(
				kvp => getPropertyName(kvp.Key),
				kvp => kvp.Value.ToArray()),
		});
	}
}