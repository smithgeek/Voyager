using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Voyager.Api
{
	public interface BaseEndpointRequest
	{
	}

	public interface EndpointRequest : IRequest<IActionResult>, BaseEndpointRequest
	{
	}

	public interface EndpointRequest<TBody> : IRequest<ActionResult<TBody>>, BaseEndpointRequest
	{
	}
}