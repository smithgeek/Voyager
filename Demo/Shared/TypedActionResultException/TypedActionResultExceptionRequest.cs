using Voyager.Api;

namespace DemoFunctionsApp.TypedActionResultException
{
	[Route(HttpMethod.Get, "test/typedactionresult/exception")]
	public class TypedActionResultExceptionRequest : EndpointRequest<bool>
	{
	}
}