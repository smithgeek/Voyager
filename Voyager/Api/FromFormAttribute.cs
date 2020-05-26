using System;

namespace Voyager.Api
{
	public class FromFormAttribute : Attribute
	{
		public FromFormAttribute()
		{
		}

		public FromFormAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}
}