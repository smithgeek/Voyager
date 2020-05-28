using System;
using System.Reflection;

namespace Voyager.SetProperties
{
	internal class GuidSetPropertyValue : SetPropertyValue<Guid>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (Guid.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}