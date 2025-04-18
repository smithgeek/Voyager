namespace Voyager.Validation.FluentValidation;

using global::FluentValidation;
using System.Linq.Expressions;

internal class FluentValidationAdapter<TRequest>(AbstractValidator<TRequest> validator) : IVoyagerValidationAdapter<TRequest>
{
	public async Task<ValidationSummary> Validate(TRequest request, Func<string, string> getPropertyName)
	{
		var result = await validator.ValidateAsync(request);
		return new ValidationSummary(result)
		{
			IsValid = result.IsValid,
			Errors = result.Errors.GroupBy(x => getPropertyName(x.PropertyName))
				.ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()),
		};
	}

	public void NotNull<TProperty>(Expression<Func<TRequest, TProperty>> selector, string name)
	{
		validator.RuleFor(selector).NotNull();
	}
}