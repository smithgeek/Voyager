using System.Reflection;

namespace Voyager.SetProperties
{
	internal class DecimalSetPropertyValue : SetPropertyValue<decimal>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (decimal.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}