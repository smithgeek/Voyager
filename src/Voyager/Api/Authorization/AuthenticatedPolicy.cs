using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Voyager.Api.Authorization.Requirements.Authenticated;

namespace Voyager.Api.Authorization
{
	public class AuthenticatedPolicy : Policy
	{
		public IList<IAuthorizationRequirement> GetRequirements()
		{
			return new List<IAuthorizationRequirement>
			{
				new AuthenticatedRequirement()
			};
		}
	}
}