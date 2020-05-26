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
			return (SetPropertyValue)serviceProvider.GetService(typeof(SetPropertyValue<>).MakeGenericType(propertyType));
		}
	}
}