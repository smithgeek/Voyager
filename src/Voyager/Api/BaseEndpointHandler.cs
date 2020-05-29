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
		public IAuthorizationService AuthorizationService { get; set; }
		public IHttpContextAccessor HttpContextAccessor { get; set; }
		public IEnumerable<string> PolicyNames { get; set; }

		public ClaimsPrincipal GetUser()
		{
			return HttpContextAccessor.HttpContext.User;
		}

		public async Task<TActionResult> Handle(TRequest request, CancellationToken cancellationToken)
		{
			var policyNames = PolicyNames;
			if (!policyNames.Any())
			{
				return await HandleRequestInternal(request, cancellationToken);
			}
			foreach (var policyName in policyNames)
			{
				AuthorizationResult result;
				if (request is ResourceRequest resourceRequest)
				{
					result = await AuthorizationService.AuthorizeAsync(HttpContextAccessor.HttpContext.User, resourceRequest.GetResource(), policyName);
				}
				else
				{
					result = await AuthorizationService.AuthorizeAsync(HttpContextAccessor.HttpContext.User, policyName);
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