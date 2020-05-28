using System.Reflection;

namespace Voyager.SetProperties
{
	internal class CharSetPropertyValue : SetPropertyValue<char>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			property.SetValue(instance, text[0]);
		}
	}
}