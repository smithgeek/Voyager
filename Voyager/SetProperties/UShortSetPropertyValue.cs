using System.Reflection;

namespace Voyager.SetProperties
{
	internal class UShortSetPropertyValue : SetPropertyValue<ushort>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (ushort.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}