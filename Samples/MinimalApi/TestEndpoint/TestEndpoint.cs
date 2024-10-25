using Voyager;

namespace Shared.TestEndpoint;

[VoyagerEndpoint("/test")]
public class TestEndpointHandler
{
	public required HttpContext HttpContext { get; set; }

	public TestEndpointResponse Post(TestEndpointRequest request)
	{
		return new TestEndpointResponse
		{
			Status = "Success",
			Id = HttpContext.TraceIdentifier,
			Message = $"{request.Other} {string.Join(", ", request.List)}"
		};
	}
}

[VoyagerEndpoint("/test2")]
public class TestEndpoint2
{
	public required CancellationToken CancellationToken { get; set; }
	public required HttpContext HttpContext { get; set; }

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder.WithDescription("Some description");
	}

	public TestEndpointResponse Post(TestEndpointRequest request, HttpContext context3, CancellationToken cancel2)
	{
		return new TestEndpointResponse
		{
			Status = "Success",
			Id = HttpContext.TraceIdentifier,
			Message = $"{request.Other} {string.Join(", ", request.List)}"
		};
	}
}

[VoyagerEndpoint("/anonymousResponse")]
public class AnonymousResponse
{
	public IResult Get(Body request)
	{
		if (request.Test == null)
		{
			return TypedResults.Ok(new
			{
				something = "here"
			});
		}
		return TypedResults.Ok(new
		{
			result = request.Test
		});
	}

	public class Body
	{
		public string? Test { get; init; }
	}
}

//[VoyagerEndpoint("/multipleInjections")]
//public class MultipleInjections
//{
//	public IResult Get(Service service)
//	{
//		return TypedResults.Ok();
//	}

//	public IResult Post(Service service)
//	{
//		return TypedResults.Ok();
//	}
//}