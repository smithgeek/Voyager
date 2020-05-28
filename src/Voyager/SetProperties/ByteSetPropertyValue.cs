using System.Reflection;

namespace Voyager.SetProperties
{
	internal class ByteSetPropertyValue : SetPropertyValue<byte>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (byte.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}