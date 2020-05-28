using System.Reflection;

namespace Voyager.SetProperties
{
	internal class FloatSetPropertyValue : SetPropertyValue<float>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (float.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}