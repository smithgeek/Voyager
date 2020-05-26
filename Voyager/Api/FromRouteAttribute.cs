using System;

namespace Voyager.Api
{
	public class FromRouteAttribute : Attribute
	{
		public FromRouteAttribute()
		{
		}

		public FromRouteAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}
}