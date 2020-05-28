using System.Reflection;

namespace Voyager.SetProperties
{
	internal class LongSetPropertyValue : SetPropertyValue<long>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (long.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}