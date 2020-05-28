using System;
using System.Reflection;

namespace Voyager.SetProperties
{
	internal class VersionSetPropertyValue : SetPropertyValue<Version>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (Version.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}