using System.Reflection;

namespace Voyager.SetProperties
{
	internal class UIntSetPropertyValue : SetPropertyValue<uint>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (uint.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}