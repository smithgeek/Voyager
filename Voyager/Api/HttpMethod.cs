namespace Voyager.Api
{
	public enum HttpMethod : byte
	{
		Get = 0,
		Put = 1,
		Delete = 2,
		Post = 3,
		Head = 4,
		Trace = 5,
		Patch = 6,
		Connect = 7,
		Options = 8,
		Custom = 9,
		None = 255
	}
}