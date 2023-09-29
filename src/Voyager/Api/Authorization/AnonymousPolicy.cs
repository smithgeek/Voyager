using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace Voyager.Api.Authorization
{
	public class AnonymousPolicy : Policy
	{
		public IList<IAuthorizationRequirement>? GetRequirements()
		{
			return null;
		}
	}
}