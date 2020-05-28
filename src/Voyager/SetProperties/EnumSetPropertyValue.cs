using System;

namespace Voyager.SetProperties
{
	public interface EnumSetPropertyValue<TEnum> : SetPropertyValue where TEnum : Enum
	{
	}
}