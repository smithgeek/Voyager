using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Voyager.Api
{
	public interface EndpointRequest : IRequest<IActionResult>
	{
	}

	public interface EndpointRequest<TBody> : IRequest<ActionResult<TBody>>
	{
	}
}