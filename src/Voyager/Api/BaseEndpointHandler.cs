using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Voyager.Api
{
	public abstract class BaseEndpointHandler<TRequest, TActionResult> : IRequestHandler<TRequest, TActionResult>, InjectEndpointProps
		where TRequest : IRequest<TActionResult>
	{
		IAuthorizationService InjectEndpointProps.AuthorizationService { get; set; }
		IHttpContextAccessor InjectEndpointProps.HttpContextAccessor { get; set; }
		IEnumerable<string> InjectEndpointProps.PolicyNames { get; set; }

		public ClaimsPrincipal GetUser()
		{
			return ((InjectEndpointProps)this).HttpContextAccessor.HttpContext.User;
		}

		public async Task<TActionResult> Handle(TRequest request, CancellationToken cancellationToken)
		{
			var policyNames = ((InjectEndpointProps)this).PolicyNames;
			if (!policyNames.Any())
			{
				return await HandleRequestInternal(request, cancellationToken);
			}
			foreach (var policyName in policyNames)
			{
				AuthorizationResult result;
				if (request is ResourceRequest resourceRequest)
				{
					result = await ((InjectEndpointProps)this).AuthorizationService.AuthorizeAsync(((InjectEndpointProps)this).HttpContextAccessor.HttpContext.User, resourceRequest.GetResource(), policyName);
				}
				else
				{
					result = await ((InjectEndpointProps)this).AuthorizationService.AuthorizeAsync(((InjectEndpointProps)this).HttpContextAccessor.HttpContext.User, policyName);
				}
				if (!result.Succeeded)
				{
					return GetUnathorizedResponse();
				}
			}
			return await HandleRequestInternal(request, cancellationToken);
		}

		internal abstract TActionResult GetUnathorizedResponse();

		internal abstract Task<TActionResult> HandleRequestInternal(TRequest request, CancellationToken cancellation);
	}
}