using System.Reflection;

namespace Voyager.SetProperties
{
	internal class ULongSetPropertyValue : SetPropertyValue<ulong>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (ulong.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}