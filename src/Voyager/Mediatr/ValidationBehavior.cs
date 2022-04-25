using FluentValidation;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Voyager.Mediatr
{
	public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
		where TRequest : IRequest<TResponse>
	{
		private readonly IEnumerable<IValidator<TRequest>> validators;

		public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
		{
			this.validators = validators;
		}

		public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
		{
			var validationContext = new ValidationContext<TRequest>(request);
			var failures = validators.Select(v => v.Validate(validationContext))
				.SelectMany(result => result.Errors).Where(failure => failure != null);
			if (failures.Any())
			{
				throw new ValidationException(failures);
			}
			return next();
		}
	}
}