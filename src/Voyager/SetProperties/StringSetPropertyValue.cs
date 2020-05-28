using System.Reflection;

namespace Voyager.SetProperties
{
	internal class StringSetPropertyValue : SetPropertyValue<string>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			property.SetValue(instance, text);
		}
	}
}