using System;
using System.Reflection;

namespace Voyager.SetProperties
{
	internal class TimeSpanSetPropertyValue : SetPropertyValue<TimeSpan>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (TimeSpan.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}