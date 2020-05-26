using System;
using System.Reflection;

namespace Voyager.SetProperties
{
	internal class EnumValueSetPropertyValue<TEnum> : EnumSetPropertyValue<TEnum> where TEnum : Enum
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (Enum.TryParse(typeof(TEnum), text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}