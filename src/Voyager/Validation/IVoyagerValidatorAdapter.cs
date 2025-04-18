using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Voyager.Validation;
public interface IVoyagerValidationAdapter<TRequest>
{
	public Task<ValidationSummary> Validate(TRequest request, Func<string, string> getPropertyName);

	public void NotNull<TProperty>(Expression<Func<TRequest, TProperty>> selector, string name)
	{

	}
}
