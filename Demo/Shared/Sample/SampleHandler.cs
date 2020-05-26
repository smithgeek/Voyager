using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voyager.Api;
using Voyager.Api.Authorization;

namespace DemoFunctionsApp.Sample
{
	public class SampleHandler : EndpointHandler<SampleRequest, SampleResponse, AnonymousPolicy>
	{
		public SampleHandler(IHttpContextAccessor httpContextAccessor)
			: base(httpContextAccessor)
		{
		}

		public override ActionResult<SampleResponse> HandleRequest(SampleRequest request)
		{
			if (request.Value == "exception")
			{
				throw new System.Exception("This is an exception");
			}
			return Ok(new SampleResponse { Test = "success" });
		}
	}
}