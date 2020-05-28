using System.Reflection;

namespace Voyager.SetProperties
{
	internal class SByteSetPropertyValue : SetPropertyValue<sbyte>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (sbyte.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}