namespace Voyager;

public static class VoyagerEndpoints
{
	public static void AddVoyagerServices(IServiceCollection services)
	{
		AddVoyager(services);
	}

	public static void MapVoyagerEndpoints(WebApplication app)
	{
		MapVoyager(app);
	}

	internal static void AddVoyager(this IServiceCollection services)
	{
	}

	internal static void MapVoyager(this WebApplication app)
	{
	}
}