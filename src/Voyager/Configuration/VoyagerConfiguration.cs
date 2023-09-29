using System;

namespace Voyager.Configuration
{
	public class VoyagerConfiguration
	{
		public string? EnvironmentName { get; set; }

		public bool IsDevelopment()
		{
			return EnvironmentName?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true;
		}
	}
}