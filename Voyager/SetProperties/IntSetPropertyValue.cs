using System.Reflection;

namespace Voyager.SetProperties
{
	internal class IntSetPropertyValue : SetPropertyValue<int>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (int.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}