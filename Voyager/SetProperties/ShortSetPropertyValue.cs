using System.Reflection;

namespace Voyager.SetProperties
{
	internal class ShortSetPropertyValue : SetPropertyValue<short>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (short.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}