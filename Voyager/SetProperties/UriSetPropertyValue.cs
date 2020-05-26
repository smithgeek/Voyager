using System;
using System.Reflection;

namespace Voyager.SetProperties
{
	internal class UriSetPropertyValue : SetPropertyValue<Uri>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			var uri = new Uri(text);
			property.SetValue(instance, uri);
		}
	}
}