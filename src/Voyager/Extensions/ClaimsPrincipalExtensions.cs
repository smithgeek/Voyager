using System;
using System.Linq;
using System.Security.Claims;

namespace Voyager.Api
{
	public static class ClaimsPrincipalExtensions
	{
		public static string TryGetClaim(this ClaimsPrincipal principal, string claimType)
		{
			if (principal != null)
			{
				var claim = principal.Claims.FirstOrDefault(c => c.Type == claimType);
				if (claim != null)
				{
					return claim.Value;
				}
			}
			return null;
		}

		public static Guid? TryGetGuidClaimValue(this ClaimsPrincipal principal, string claimType)
		{
			var claimValue = principal.TryGetClaim(claimType);
			return claimValue == null ? (Guid?)null : new Guid(claimValue);
		}

		public static Guid? TryGetImpersonatedUserId(this ClaimsPrincipal principal)
		{
			return principal.TryGetGuidClaimValue("impersonatedBy");
		}

		public static Guid? TryGetUserId(this ClaimsPrincipal principal)
		{
			return principal.TryGetGuidClaimValue(ClaimTypes.NameIdentifier) ?? principal.TryGetGuidClaimValue("sub");
		}
	}
}