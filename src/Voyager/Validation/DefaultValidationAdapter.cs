using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Voyager.Validation;

internal class DefaultValidationAdapter<TRequest>() : IVoyagerValidationAdapter<TRequest>
{
	private readonly List<Func<TRequest, string?>> expressions = [];

	public void NotNull<TProperty>(Expression<Func<TRequest, TProperty>> selector, string name)
	{
		var nullCheck = Expression.Equal(selector.Body, Expression.Constant(null, typeof(TProperty)));
		var propNames = Expression.Condition(nullCheck, Expression.Constant(name), Expression.Constant(null, typeof(string)));
		expressions.Add(Expression.Lambda<Func<TRequest, string?>>(propNames, selector.Parameters[0]).Compile());
	}

	public Task<ValidationSummary> Validate(TRequest request, Func<string, string> getPropertyName)
	{
		var invalidProps = (List<string>)expressions.Select(exp => exp(request)).Where(s => s != null).ToList()!;
		return Task.FromResult(new ValidationSummary
		{
			IsValid = invalidProps.Count == 0,
			Errors = invalidProps.ToDictionary(p => getPropertyName(p), p => (string[])["Must not be null"])
		});
	}
}