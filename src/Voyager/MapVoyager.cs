using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voyager;

namespace Microsoft.AspNetCore.Builder;

public static class MapVoyagerExtension
{
	public static WebApplication MapVoyager(this WebApplication app)
	{
		var mappings = app.Services.GetService<IEnumerable<IVoyagerMapping>>();
		if (mappings is not null)
		{
			foreach (var mapping in mappings)
			{
				mapping.MapEndpoints(app);
			}
		}
		return app;
	}
}
