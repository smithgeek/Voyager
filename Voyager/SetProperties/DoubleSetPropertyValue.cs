using System.Reflection;

namespace Voyager.SetProperties
{
	internal class DoubleSetPropertyValue : SetPropertyValue<double>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			if (double.TryParse(text, out var value))
			{
				property.SetValue(instance, value);
			}
		}
	}
}