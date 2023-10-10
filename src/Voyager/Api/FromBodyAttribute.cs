using System;

namespace Voyager.Api
{
	public class FromBodyAttribute : Attribute
	{
		public FromBodyAttribute()
		{
		}

		public FromBodyAttribute(string name)
		{
			Name = name;
		}

		public string? Name { get; set; }
	}
}