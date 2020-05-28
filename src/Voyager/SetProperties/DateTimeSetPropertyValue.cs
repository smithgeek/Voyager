using System;
using System.Reflection;

namespace Voyager.SetProperties
{
	internal class DateTimeSetPropertyValue : SetPropertyValue<DateTime>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (DateTime.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}