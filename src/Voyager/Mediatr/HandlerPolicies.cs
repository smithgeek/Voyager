using MediatR;
using System.Collections.Generic;

namespace Voyager.Mediatr
{
	internal class HandlerPolicies<TRequest, TResponse> : PolicyList where TRequest : IRequest<TResponse>
	{
		public IEnumerable<string> PolicyNames { get; set; }
	}
}