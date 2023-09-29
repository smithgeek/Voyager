using System;

namespace Voyager.Api
{
	public class FromQueryAttribute : Attribute
	{
		public FromQueryAttribute()
		{
		}

		public FromQueryAttribute(string name)
		{
			Name = name;
		}

		public string? Name { get; set; }
	}
}