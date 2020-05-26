using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using Voyager.Api.Authorization;

namespace Voyager.Api
{
	public abstract class BaseEndpointHandler<TRequest, TActionResult, TPolicy> : IRequestHandler<TRequest, TActionResult>
		where TRequest : IRequest<TActionResult>
		where TPolicy : Policy
	{
		private readonly IAuthorizationService authorizationService;
		private readonly IHttpContextAccessor httpContextAccessor;

		public BaseEndpointHandler(IHttpContextAccessor httpContextAccessor)
		{
			authorizationService = (IAuthorizationService)httpContextAccessor.HttpContext.RequestServices.GetService(typeof(IAuthorizationService));
			this.httpContextAccessor = httpContextAccessor;
		}

		public async Task<TActionResult> Handle(TRequest request, CancellationToken cancellationToken)
		{
			var policyName = typeof(TPolicy).FullName;
			if (policyName == null)
			{
				return await HandleRequestInternal(request, cancellationToken);
			}
			var result = await authorizationService.AuthorizeAsync(httpContextAccessor.HttpContext.User, policyName);
			if (result.Succeeded)
			{
				return await HandleRequestInternal(request, cancellationToken);
			}
			else
			{
				return GetUnathorizedResponse();
			}
		}

		internal abstract TActionResult GetUnathorizedResponse();

		internal abstract Task<TActionResult> HandleRequestInternal(TRequest request, CancellationToken cancellation);
	}
}