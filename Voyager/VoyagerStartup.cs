using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Voyager.Api;
using Voyager.Api.Authorization;
using Voyager.Configuration;
using Voyager.Mediatr;
using Voyager.Middleware;
using Voyager.SetProperties;

[assembly: InternalsVisibleTo("Voyager.Tests")]

namespace Voyager
{
	internal static class VoyagerStartup
	{
		public static void Configure(VoyagerConfigurationBuilder builder, IServiceCollection services)
		{
			services.AddSingleton<ExceptionHandler>();
			var voyagerConfig = new VoyagerConfiguration
			{
				EnvironmentName = Environment.GetEnvironmentVariable("VOYAGER_ENVIRONMENT") ??
					Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "DEVELOPMENT"
			};
			services.AddSingleton(voyagerConfig);
			foreach (var assembly in builder.Assemblies)
			{
				services.AddMediatR(assembly);
			}
			builder.AddAssemblyWith<VoyagerConfigurationBuilder>();
			services.AddHttpContextAccessor();
			services.AddScoped<AppRouter>();
			services.TryAddSingleton<ModelBinder, DefaultModelBinder>();
			services.TryAddTransient<PropertySetterFactory, DefaultPropertySetterFactory>();
			AddPropertySetters(services, builder.Assemblies);
			RegisterMediatorHandlers(services, builder.Assemblies);
			RegisterMediatorRequests(services, builder.Assemblies);
			AddCustomAuthorization(services, builder.Assemblies);
			services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
			services.AddValidatorsFromAssemblies(builder.Assemblies);
			services.AddLogging();
		}

		internal static IEnumerable<Type> GetAllTypesImplementingType(Type openGenericType, Assembly assembly)
		{
			return from x in assembly.GetTypes()
				   from z in x.GetInterfaces()
				   let y = x.BaseType
				   where
				   y != null && y.IsGenericType &&
				   openGenericType.IsAssignableFrom(y.GetGenericTypeDefinition()) ||
				   z.IsGenericType &&
				   openGenericType.IsAssignableFrom(z.GetGenericTypeDefinition())
				   select x;
		}

		internal static void RegisterMediatorHandlers(IServiceCollection services, IEnumerable<Assembly> assemblies)
		{
			foreach (var assembly in assemblies)
			{
				foreach (var type in GetAllTypesImplementingType(typeof(IRequestHandler<,>), assembly))
				{
					var interfaceType = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
					if (interfaceType != null && !type.IsAbstract)
					{
						var name = type.Name;
						if (type.IsGenericType)
						{
							services.AddScoped(type.GetGenericTypeDefinition());
							services.TryAddScoped(interfaceType.GetGenericTypeDefinition(), type.GetGenericTypeDefinition());
						}
						else
						{
							services.AddScoped(type);
							services.TryAddScoped(interfaceType, type);
						}
					}
				}
			}
		}

		internal static void RegisterMediatorRequests(IServiceCollection services, IEnumerable<Assembly> assemblies)
		{
			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.GetTypes().Where(t => typeof(IBaseRequest).IsAssignableFrom(t)))
				{
					if (!type.IsInterface && !type.IsAbstract)
					{
						if (type.IsGenericType)
						{
							services.AddScoped(type.GetGenericTypeDefinition());
						}
						else
						{
							services.AddScoped(type);
						}
						var routeAttributes = type.GetCustomAttributes<RouteAttribute>();
						foreach (var routeAttribute in routeAttributes)
						{
							services.AddTransient((serviceProvider) => new EndpointRoute
							{
								Method = routeAttribute.Method,
								TemplateMatcher = routeAttribute.TemplateMatcher,
								RequestType = type,
								Template = routeAttribute.Template
							});
						}
					}
				}
			}
		}

		private static void AddCustomAuthorization(IServiceCollection services, IEnumerable<Assembly> assemblies)
		{
			services.AddAuthorizationCore(options =>
			{
				foreach (var assembly in assemblies)
				{
					foreach (var policyType in assembly.GetTypes().Where(t => !t.IsInterface && typeof(Policy).IsAssignableFrom(t)))
					{
						var policy = (Policy)Activator.CreateInstance(policyType);
						options.AddPolicy(policyType.FullName, policyBuilder =>
						{
							var requirements = policy.GetRequirements();
							if (requirements is null || requirements.Count == 0)
							{
								policyBuilder.RequireAssertion(c => { return true; });
							}
							else
							{
								policyBuilder.Requirements = policy.GetRequirements();
							}
						});
					}
				}
			});
			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.GetTypes().Where(t => typeof(IAuthorizationHandler).IsAssignableFrom(t)))
				{
					services.AddScoped(typeof(IAuthorizationHandler), type);
				}
			}
		}

		private static void AddPropertySetters(IServiceCollection services, IEnumerable<Assembly> assemblies)
		{
			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(SetPropertyValue).IsAssignableFrom(t)))
				{
					if (type.IsGenericType)
					{
						var serviceType = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType);
						if (serviceType != null)
						{
							services.TryAddSingleton(serviceType.GetGenericTypeDefinition(), type);
						}
					}
					else
					{
						var serviceType = type.GetInterfaces().FirstOrDefault(i => typeof(SetPropertyValue<>) == i.GetGenericTypeDefinition());
						if (serviceType != null)
						{
							services.TryAddSingleton(serviceType, type);
						}
					}
				}
			}
		}
	}
}