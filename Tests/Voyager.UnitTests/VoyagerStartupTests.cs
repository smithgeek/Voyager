using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voyager.Api.Authorization;
using Voyager.AssemblyFactories;
using Voyager.Middleware;
using Xunit;

namespace Voyager.UnitTests
{
	public class AuthenticatedPolicyOverride : OverridePolicy<AuthenticatedPolicy>
	{
		public IList<IAuthorizationRequirement> GetRequirements()
		{
			return new[]
			{
				new CustomAppRequirement()
			};
		}
	}

	public class BlankPolicy : Policy
	{
		public IList<IAuthorizationRequirement> GetRequirements()
		{
			return new List<IAuthorizationRequirement>();
		}
	}

	public class CustomAppRequirement : IAuthorizationRequirement
	{ }

	public class VoyagerStartupTests
	{
		[Fact]
		public void FindPolicies()
		{
			var definitions = Voyager.AssemblyFactories.Registration.GetPolicies();
			definitions.Count().Should().Be(2);
		}

		[Fact]
		public async Task PoliciesCanBeOverwritten()
		{
			var services = new ServiceCollection();
			services.AddVoyager(new AddVoyagerOptions
			{
				RegisterPolicies = policies =>
				{
					policies[typeof(AuthenticatedPolicy).FullName] = new AuthenticatedPolicyOverride();
				}
			});
			var providier = services.BuildServiceProvider();
			var policyProvider = providier.GetRequiredService<IAuthorizationPolicyProvider>();
			var policy = await policyProvider.GetPolicyAsync(typeof(AuthenticatedPolicy).FullName);

			var expectedRequirements = new AuthenticatedPolicyOverride().GetRequirements();
			var actualRequirements = policy.Requirements;
			actualRequirements.Count.Should().Be(1);
			actualRequirements.First().GetType().Should().Be<CustomAppRequirement>();
		}

		[Fact]
		public void RunningStartupMultipleTimesIsOk()
		{
			var services = new ServiceCollection();
			services.AddVoyager();
			services.AddVoyager();
			var instance = services.BuildServiceProvider().GetService<ExceptionHandler>();
			instance.Should().NotBeNull();
		}
	}
}