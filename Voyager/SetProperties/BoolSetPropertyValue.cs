using System.Reflection;

namespace Voyager.SetProperties
{
	internal class BoolSetPropertyValue : SetPropertyValue<bool>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (bool.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}