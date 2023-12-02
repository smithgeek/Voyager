using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Voyager.Api
{
	public abstract class EndpointHandler<TRequest> : IEndpointHandler<TRequest>
			where TRequest : IRequest<IActionResult>
	{
		[VoyagerInject]
		public virtual HttpContext? HttpContext { get; set; }

		public virtual ClaimsPrincipal? User { get => HttpContext?.User; }

		public virtual Task<IActionResult> Handle(TRequest request, CancellationToken cancellation)
		{
			return HandleRequestAsync(request, cancellation);
		}

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

		protected IActionResult BadRequest()
		{
			return new BadRequestResult();
		}

		protected IActionResult BadRequest(ProblemDetails details)
		{
			details.Status = (int)HttpStatusCode.BadRequest;
			return new BadRequestObjectResult(details);
		}

		protected async Task<IActionResult> BadRequest(Task<ProblemDetails> details)
		{
			return BadRequest(await details);
		}

		protected IActionResult NotFound()
		{
			return new NotFoundResult();
		}

		protected IActionResult NotFound(ProblemDetails details)
		{
			details.Status = (int)HttpStatusCode.NotFound;
			return new NotFoundObjectResult(details);
		}

		protected async Task<IActionResult> NotFound(Task<ProblemDetails> details)
		{
			return NotFound(await details);
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
			details.Status = (int)HttpStatusCode.Unauthorized;
			return new UnauthorizedObjectResult(details);
		}

		protected async Task<IActionResult> Unauthorized(Task<ProblemDetails> details)
		{
			return Unauthorized(await details);
		}
	}

	public abstract class EndpointHandler<TRequest, TResponse> : IEndpointHandler<TRequest, TResponse>
		where TRequest : IRequest<ActionResult<TResponse>>
	{
		[VoyagerInject]
		public virtual HttpContext? HttpContext { get; set; }

		public virtual ClaimsPrincipal? User { get => HttpContext?.User; }

		public virtual Task<ActionResult<TResponse>> Handle(TRequest request, CancellationToken cancellation)
		{
			return HandleRequestAsync(request, cancellation);
		}

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

		protected ActionResult<TResponse> BadRequest()
		{
			return new BadRequestResult();
		}

		protected ActionResult<TResponse> BadRequest(ProblemDetails details)
		{
			details.Status = (int)HttpStatusCode.BadRequest;
			return new BadRequestObjectResult(details);
		}

		protected async Task<ActionResult<TResponse>> BadRequest(Task<ProblemDetails> detailsTask)
		{
			return BadRequest(await detailsTask);
		}

		protected IActionResult NotFound()
		{
			return new NotFoundResult();
		}

		protected IActionResult NotFound(ProblemDetails details)
		{
			details.Status = (int)HttpStatusCode.NotFound;
			return new NotFoundObjectResult(details);
		}

		protected async Task<IActionResult> NotFound(Task<ProblemDetails> details)
		{
			return NotFound(await details);
		}

		protected ActionResult<TResponse> Ok(TResponse response)
		{
			return new OkObjectResult(response);
		}

		protected async Task<ActionResult<TResponse>> Ok(Task<TResponse> response)
		{
			return Ok(await response);
		}

		protected ActionResult<TResponse> Unauthorized()
		{
			return new UnauthorizedResult();
		}

		protected ActionResult<TResponse> Unauthorized(ProblemDetails details)
		{
			details.Status = (int)HttpStatusCode.Unauthorized;
			return new UnauthorizedObjectResult(details);
		}

		protected async Task<ActionResult<TResponse>> Unauthorized(Task<ProblemDetails> details)
		{
			return Unauthorized(await details);
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class VoyagerInjectAttribute : Attribute
	{ }
}