using System;

namespace Voyager.SetProperties
{
	public interface PropertySetterFactory
	{
		SetPropertyValue Get(Type propertyType);
	}
}