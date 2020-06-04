using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
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

[assembly: InternalsVisibleTo("Voyager.UnitTests")]

namespace Voyager
{
	internal static class VoyagerStartup
	{
		public static void Configure(VoyagerConfigurationBuilder builder, IServiceCollection services)
		{
			var isRegistered = services.Any(s => s.ImplementationType == typeof(IsRegistered));
			if (!isRegistered)
			{
				services.AddScoped<IsRegistered>();
				services.AddMvcCore().AddApiExplorer();
				services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, VoyagerApiDescriptionProvider>());
				services.AddSingleton<ExceptionHandler, DefaultExceptionHandler>();
				var voyagerConfig = new VoyagerConfiguration
				{
					EnvironmentName = Environment.GetEnvironmentVariable("VOYAGER_ENVIRONMENT") ??
						Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "DEVELOPMENT"
				};
				services.AddSingleton(voyagerConfig);
				builder.AddAssemblyWith<VoyagerConfigurationBuilder>();
				services.AddHttpContextAccessor();
				services.AddScoped<AppRouter>();
				services.TryAddSingleton<ModelBinder, DefaultModelBinder>();
				services.TryAddTransient<PropertySetterFactory, DefaultPropertySetterFactory>();
				AddPropertySetters(services, builder.Assemblies);
				services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
				services.AddValidatorsFromAssemblies(builder.Assemblies);
				services.AddLogging();
				services.AddSingleton<TypeBindingRepository>();
			}

			RegisterMediatorHandlers(services, builder.Assemblies);
			RegisterMediatorRequests(services, builder.Assemblies);
			AddCustomAuthorization(services, builder.Assemblies);

			foreach (var assembly in builder.Assemblies)
			{
				services.AddMediatR(assembly);
			}
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
						RegisterAuthroziationBehavior(services, type, interfaceType);

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
							services.AddTransient((serviceProvider) => routeAttribute.ToEndpointRoute(type));
						}
					}
				}
			}
		}

		private static void AddCustomAuthorization(IServiceCollection services, IEnumerable<Assembly> assemblies)
		{
			var policies = new List<Policy>();
			foreach (var assembly in assemblies)
			{
				foreach (var policyType in assembly.GetTypes().Where(t => !t.IsInterface && typeof(Policy).IsAssignableFrom(t)))
				{
					services.AddScoped(policyType);
					policies.Add((Policy)Activator.CreateInstance(policyType));
				}
			}

			services.AddAuthorizationCore(options =>
			{
				foreach (var policy in policies)
				{
					options.AddPolicy(policy.GetType().FullName, policyBuilder =>
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

		private static void RegisterAuthroziationBehavior(IServiceCollection services, Type type, Type interfaceType)
		{
			var policies = type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Enforce<>));
			if (policies.Any())
			{
				var requestResponseTypes = interfaceType.GetGenericArguments();
				var requestType = requestResponseTypes[0];
				var responseType = requestResponseTypes[1];
				var handlerPoliciesType = typeof(HandlerPolicies<,>).MakeGenericType(requestType, responseType);
				services.TryAddScoped(handlerPoliciesType, provider =>
				{
					var policyList = (PolicyList)Activator.CreateInstance(handlerPoliciesType);
					policyList.PolicyNames = policies.Select(p => p.GetGenericArguments()[0].FullName);
					return policyList;
				});
				if (typeof(Microsoft.AspNetCore.Mvc.IActionResult).IsAssignableFrom(responseType))
				{
					services.TryAddScoped(typeof(UnauthorizedResponseFactory<>).MakeGenericType(responseType), typeof(IActionResultUnauthorizedResponseFactory<>).MakeGenericType(responseType));
				}
				else if (typeof(Microsoft.AspNetCore.Mvc.ActionResult<>).IsAssignableFrom(responseType.GetGenericTypeDefinition()))
				{
					services.TryAddScoped(typeof(UnauthorizedResponseFactory<>).MakeGenericType(responseType),
						typeof(ActionResultUnauthorizedResponseFactory<>).MakeGenericType(responseType.GetGenericArguments()[0]));
				}
				else
				{
					throw new Exception($"Unknown response type: {responseType.FullName}. unable to return an unauthorized response");
				}
				services.TryAddScoped(typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType),
					typeof(AuthorizationBehavior<,>).MakeGenericType(requestType, responseType));
			}
		}

		// Used to keep track if voyager registration has run at least once. It can be run multiple times with different assemblies.
		private class IsRegistered { }
	}
}