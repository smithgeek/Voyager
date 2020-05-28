using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Voyager.Api;

namespace SampleFunctionsApp
{
	public class Routes
	{
		private readonly HttpRouter router;

		public Routes(HttpRouter router)
		{
			this.router = router;
		}

		[FunctionName(nameof(FallbackRoute))]
		public Task<IActionResult> FallbackRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", "post", "head", "trace", "patch", "connect", "options", Route = "{*rest}")] HttpRequest req)
		{
			return router.Route(req.HttpContext);
		}
	}
}