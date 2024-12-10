using Microsoft.AspNetCore.Mvc;
using Voyager;

namespace ClassLibrary;

public class Request
{
	public string? Name { get; set; }
}

[VoyagerEndpoint("/test")]
public class Handler
{
	public bool Post(Request request)
	{
		return true;
	}
}

public class Request2
{
	public string? Name { get; set; }

	[FromRoute]
	public int Index { get; set; }
}

[VoyagerEndpoint("/test/{index}")]
public class Handler2
{
	public bool Post(Request2 request)
	{
		return true;
	}
}