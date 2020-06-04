using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Voyager.Api
{
	public interface IEndpointHandler<TRequest> : IRequestHandler<TRequest, IActionResult>
		where TRequest : IRequest<IActionResult>
	{
	}

	public interface IEndpointHandler<TRequest, TResponse> : IRequestHandler<TRequest, ActionResult<TResponse>>
	where TRequest : IRequest<ActionResult<TResponse>>
	{
	}
}