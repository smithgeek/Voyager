using System;

namespace Voyager.SetProperties
{
	public class DefaultPropertySetterFactory : PropertySetterFactory
	{
		private readonly IServiceProvider serviceProvider;

		public DefaultPropertySetterFactory(IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
		}

		public SetPropertyValue Get(Type propertyType)
		{
			if (propertyType.IsEnum)
			{
				return (SetPropertyValue)serviceProvider.GetService(typeof(EnumSetPropertyValue<>).MakeGenericType(propertyType));
			}
			var underlyingType = Nullable.GetUnderlyingType(propertyType);
			if (underlyingType != null)
			{
				return (SetPropertyValue)serviceProvider.GetService(typeof(SetPropertyValue<>).MakeGenericType(underlyingType));
			}
			return (SetPropertyValue)serviceProvider.GetService(typeof(SetPropertyValue<>).MakeGenericType(propertyType));
		}
	}
}