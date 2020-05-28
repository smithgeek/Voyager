using System;
using System.Reflection;

namespace Voyager.SetProperties
{
	internal class DateTimeOffsetSetPropertyValue : SetPropertyValue<DateTimeOffset>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (DateTimeOffset.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}