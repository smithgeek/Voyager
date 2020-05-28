using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace Voyager.Api.Authorization
{
	public interface Policy
	{
		public IList<IAuthorizationRequirement> GetRequirements();
	}
}