using System;
using System.Collections.Generic;
using System.Reflection;

namespace Voyager.Configuration
{
	public sealed class VoyagerConfigurationBuilder
	{
		internal List<Assembly> Assemblies { get; set; } = new List<Assembly>();

		public VoyagerConfigurationBuilder AddAssembly(Assembly assembly)
		{
			Assemblies.Add(assembly);
			return this;
		}

		public VoyagerConfigurationBuilder AddAssemblyWith<Type>()
		{
			return AddAssemblyWith(typeof(Type));
		}

		public VoyagerConfigurationBuilder AddAssemblyWith(Type type)
		{
			return AddAssembly(type.Assembly);
		}
	}
}