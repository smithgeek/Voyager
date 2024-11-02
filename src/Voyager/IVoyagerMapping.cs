using Microsoft.AspNetCore.Builder;

namespace Voyager;

public interface IVoyagerMapping
{
	void MapEndpoints(WebApplication app);
}
