using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Voyager.Api.Authorization.Requirements.Authenticated
{
	public class AuthenticatedHandler : AuthorizationHandler<AuthenticatedRequirement>
	{
		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthenticatedRequirement requirement)
		{
			if (context.User.Identity.IsAuthenticated)
			{
				context.Succeed(requirement);
			}
			return Task.CompletedTask;
		}
	}
}