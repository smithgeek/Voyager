namespace Voyager.Api
{
	internal interface UnauthorizedResponseFactory<TResponse>
	{
		TResponse GetUnauthorizedResponse();
	}
}