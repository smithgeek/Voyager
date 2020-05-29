using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Voyager.Api
{
	public interface InjectEndpointProps
	{
		IAuthorizationService AuthorizationService { get; set; }
		IHttpContextAccessor HttpContextAccessor { get; set; }
		IEnumerable<string> PolicyNames { get; set; }
	}
}