using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Voyager.Api
{
	abstract public class EndpointHandler<TRequest> : BaseEndpointHandler<TRequest, IActionResult>
		where TRequest : IRequest<IActionResult>
	{
		public virtual IActionResult HandleRequest(TRequest request)
		{
			throw new NotImplementedException();
		}

		public virtual Task<IActionResult> HandleRequestAsync(TRequest request, CancellationToken cancellation)
		{
			return HandleRequestAsync(request);
		}

		public virtual Task<IActionResult> HandleRequestAsync(TRequest request)
		{
			return Task.FromResult(HandleRequest(request));
		}

		internal override IActionResult GetUnathorizedResponse()
		{
			return new UnauthorizedResult();
		}

		internal override Task<IActionResult> HandleRequestInternal(TRequest request, CancellationToken cancellation)
		{
			return HandleRequestAsync(request, cancellation);
		}

		protected IActionResult BadRequest()
		{
			return new BadRequestResult();
		}

		protected IActionResult BadRequest(ProblemDetails details)
		{
			return new BadRequestObjectResult(details);
		}

		protected async Task<IActionResult> BadRequest(Task<ProblemDetails> details)
		{
			return BadRequest(await details);
		}

		protected IActionResult Ok()
		{
			return new OkResult();
		}

		protected IActionResult Unauthorized()
		{
			return new UnauthorizedResult();
		}

		protected IActionResult Unauthorized(ProblemDetails details)
		{
			return new UnauthorizedObjectResult(details);
		}

		protected async Task<IActionResult> Unauthorized(Task<ProblemDetails> details)
		{
			return Unauthorized(await details);
		}
	}

	abstract public class EndpointHandler<TRequest, TResponse> : BaseEndpointHandler<TRequest, ActionResult<TResponse>>
		where TRequest : IRequest<ActionResult<TResponse>>
	{
		public virtual ActionResult<TResponse> HandleRequest(TRequest request)
		{
			throw new NotImplementedException();
		}

		public virtual Task<ActionResult<TResponse>> HandleRequestAsync(TRequest request, CancellationToken cancellation)
		{
			return HandleRequestAsync(request);
		}

		public virtual Task<ActionResult<TResponse>> HandleRequestAsync(TRequest request)
		{
			return Task.FromResult(HandleRequest(request));
		}

		internal override ActionResult<TResponse> GetUnathorizedResponse()
		{
			return new UnauthorizedResult();
		}

		internal override Task<ActionResult<TResponse>> HandleRequestInternal(TRequest request, CancellationToken cancellation)
		{
			return HandleRequestAsync(request, cancellation);
		}

		protected ActionResult<TResponse> BadRequest()
		{
			return new BadRequestResult();
		}

		protected ActionResult<TResponse> BadRequest(ProblemDetails details)
		{
			return new BadRequestObjectResult(details);
		}

		protected async Task<ActionResult<TResponse>> BadRequest(Task<ProblemDetails> details)
		{
			return BadRequest(await details);
		}

		protected ActionResult<TResponse> Ok(TResponse response)
		{
			return response;
		}

		protected async Task<ActionResult<TResponse>> Ok(Task<TResponse> response)
		{
			return await response;
		}

		protected ActionResult<TResponse> Unauthorized()
		{
			return new UnauthorizedResult();
		}

		protected ActionResult<TResponse> Unauthorized(ProblemDetails details)
		{
			return new UnauthorizedObjectResult(details);
		}

		protected async Task<ActionResult<TResponse>> Unauthorized(Task<ProblemDetails> details)
		{
			return Unauthorized(await details);
		}
	}
}