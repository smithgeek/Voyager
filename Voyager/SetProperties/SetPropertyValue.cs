using System.Reflection;

namespace Voyager.SetProperties
{
	public interface SetPropertyValue
	{
		void SetValue(PropertyInfo property, object instance, string text);
	}

	public interface SetPropertyValue<T> : SetPropertyValue
	{
	}
}