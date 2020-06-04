using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Voyager.Api;

namespace Voyager.Mediatr
{
	internal class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
		where TRequest : IRequest<TResponse>
	{
		private readonly IAuthorizationService authorizationService;
		private readonly HandlerPolicies<TRequest, TResponse> handlerPolicies;
		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly UnauthorizedResponseFactory<TResponse> unauthorizedResponseFactory;

		public AuthorizationBehavior(HandlerPolicies<TRequest, TResponse> handlerPolicies, IAuthorizationService authorizationService,
			IHttpContextAccessor httpContextAccessor, UnauthorizedResponseFactory<TResponse> unauthorizedResponseFactory)
		{
			this.handlerPolicies = handlerPolicies;
			this.authorizationService = authorizationService;
			this.httpContextAccessor = httpContextAccessor;
			this.unauthorizedResponseFactory = unauthorizedResponseFactory;
		}

		public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
		{
			if (!handlerPolicies.PolicyNames.Any())
			{
				return await next();
			}
			foreach (var policyName in handlerPolicies.PolicyNames)
			{
				AuthorizationResult result;
				if (request is ResourceRequest resourceRequest)
				{
					result = await authorizationService.AuthorizeAsync(httpContextAccessor.HttpContext.User, resourceRequest.GetResource(), policyName);
				}
				else
				{
					result = await authorizationService.AuthorizeAsync(httpContextAccessor.HttpContext.User, policyName);
				}
				if (!result.Succeeded)
				{
					return unauthorizedResponseFactory.GetUnauthorizedResponse();
				}
			}
			return await next();
		}
	}
}