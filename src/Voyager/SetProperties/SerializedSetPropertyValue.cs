using System.Reflection;
using System.Text.Json;

namespace Voyager.SetProperties
{
	internal class SerializedSetPropertyValue<T> : SetPropertyValue<T>
	{
		public void SetValue(PropertyInfo property, object instance, string text)
		{
			var value = JsonSerializer.Deserialize<T>(text, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
			property.SetValue(instance, value);
		}
	}
}