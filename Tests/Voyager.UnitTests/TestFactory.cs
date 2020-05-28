using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Voyager.UnitTests
{
	public static class TestFactory
	{
		public static HttpContext HttpContext()
		{
			var services = new ServiceCollection();
			services.AddScoped(typeof(ILogger<>), typeof(NullLogger<>));
			services.AddScoped(typeof(ILoggerFactory), typeof(NullLoggerFactory));
			var context = new DefaultHttpContext
			{
				RequestServices = services.BuildServiceProvider()
			};
			return context;
		}
	}
}