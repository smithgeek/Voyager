using Voyager;

namespace TransitiveClassLibrary;

public class Request
{
	public string? Name { get; set; }
}

[VoyagerEndpoint("/nested/test")]
public class Handler
{
	public bool Post(Request request)
	{
		return true;
	}
}