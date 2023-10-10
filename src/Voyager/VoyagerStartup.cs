using MediatR;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Voyager.Api.Authorization;
using Voyager.Configuration;
using Voyager.Factories;
using Voyager.Mediatr;
using Voyager.Middleware;

[assembly: InternalsVisibleTo("Voyager.UnitTests")]

namespace Voyager
{
	namespace AssemblyFactories
	{
		[RequestFactory]
		public static class VoyagerFactory
		{
			public static void AddAuthorization(IServiceCollection services, Dictionary<string, Policy> policies)
			{
				services.AddAuthorization(options =>
				{
					foreach (var definition in policies)
					{
						options.AddPolicy(definition.Key, policyBuilder =>
						{
							var requirements = definition.Value.GetRequirements();
							if (requirements is null || requirements.Count == 0)
							{
								policyBuilder.RequireAssertion(c => { return true; });
							}
							else
							{
								policyBuilder.Requirements = requirements;
							}
						});
					}
				});
			}

			public static void ConfigureServices(IServiceCollection services, AddVoyagerOptions _)
			{
				var isRegistered = services.Any(s => s.ImplementationType == typeof(IsRegistered));
				if (!isRegistered)
				{
					services.AddSingleton<VoyagerOptionsHolder>();
					services.AddScoped<IsRegistered>();
					services.AddMvcCore().AddApiExplorer();
					services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, VoyagerApiDescriptionProvider>());
					services.AddSingleton<PropertyMetadataRepo>();
					services.AddSingleton<ExceptionHandler, DefaultExceptionHandler>();
					var voyagerConfig = new VoyagerConfiguration
					{
						EnvironmentName = Environment.GetEnvironmentVariable("VOYAGER_ENVIRONMENT") ??
							Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "DEVELOPMENT"
					};
					services.AddSingleton(voyagerConfig);
					services.AddHttpContextAccessor();
					services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
					services.AddLogging();
				}
			}

			public static void Register(List<VoyagerRouteRegistration> registrations)
			{
			}

			public static void RegisterPolicies(Dictionary<string, Policy> policies)
			{
				policies.Add(typeof(AuthenticatedPolicy).FullName!, new AuthenticatedPolicy());
				policies.Add(typeof(AnonymousPolicy).FullName!, new AnonymousPolicy());
			}

			// Used to keep track if voyager registration has run at least once. It can be run multiple times with different assemblies.
			private class IsRegistered
			{ }
		}
	}

	public class AddVoyagerOptions
	{
		public bool RegisterAllMediator { get; set; } = true;
		public Action<Dictionary<string, Policy>>? RegisterPolicies { get; set; } = null;
	}
}