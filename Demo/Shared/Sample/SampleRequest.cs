using Voyager.Api;

namespace DemoFunctionsApp.Sample
{
	[Route(HttpMethod.Post, "sample")]
	public class SampleRequest : EndpointRequest<SampleResponse>
	{
		[FromRoute]
		public string Action { get; set; }

		public int Number { get; set; }
		public string Value { get; set; }
	}
}