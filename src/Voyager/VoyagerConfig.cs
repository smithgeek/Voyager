using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Voyager;
public class VoyagerConfig(IServiceCollection services)
{
	public IServiceCollection Services { get; } = services;

	private readonly List<Action<IServiceCollection, IList<Assembly>>> registerAssemblyHandlers = [];
	private List<Assembly> assemblies = [];

	public void OnRegisterAssemblies(Action<IServiceCollection, IList<Assembly>> registerAssembly)
	{
		registerAssemblyHandlers.Add(registerAssembly);
	}

	public void RegisterAssemblyWithType<Type>()
	{
		assemblies.Add(typeof(Type).Assembly);
	}

	public void FinishedRegisteringAssemblies(IServiceCollection services)
	{
		if (assemblies.Count != 0)
		{
			foreach (var handler in registerAssemblyHandlers)
			{
				handler(services, assemblies);
			}
		}
	}
}
