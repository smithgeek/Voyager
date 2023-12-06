using Microsoft.AspNetCore.Mvc;
using Voyager;

namespace MinimalApi;

public class AsyncReturn 
{
	[FromQuery]
	public int SomeValue { get; set; }
}

[VoyagerEndpoint("/asyncReturnHandler")]
public class AsyncReturnHandler
{
	public async Task<IResult> Get(AsyncReturn req)
	{
		return await Task.Run(async () =>
		{
			await Task.Delay(100);
			return (IResult)(req.SomeValue == 5 ? TypedResults.Ok(0) : TypedResults.Ok("ok"));
		});
	}
}
